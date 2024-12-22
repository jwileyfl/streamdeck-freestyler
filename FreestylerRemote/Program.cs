namespace FreestylerRemote
{
    using CommandLine;
    using CommandLine.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using streamdeck_client_csharp;
    using streamdeck_client_csharp.Events;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Drawing;
    using System.Linq;
    using System.Net.Mime;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Contexts;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        private const string BaseUuid = "com.resnexsoft.freestyler.remote";
        private const string On = "255";
        private const string Off = "000";
        private const string Toggle = "255";
        private List<Tuple<string, string>> commands = new List<Tuple<string, string>>();

        public class Options
        {
            [Option("port", Required = true, HelpText = "The websocket port to connect to", SetName = "port")]
            public int Port { get; set; }

            [Option("pluginUUID", Required = true, HelpText = "The UUID of the plugin")]
            public string PluginUUID { get; set; }

            [Option("registerEvent", Required = true, HelpText = "The event triggered when the plugin is registered?")]
            public string RegisterEvent { get; set; }

            [Option("info", Required = true, HelpText = "Extra JSON launch data")]
            public string Info { get; set; }
        }

        // StreamDeck launches the plugin with these details
        // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]
        private static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
#if DEBUG
            while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }
#endif
            // The command line args parser expects all args to use `--`, so, let's append
            for (int count = 0; count < args.Length; count++)
            {
                if (args[count].StartsWith("-") && !args[count].StartsWith("--"))
                {
                    args[count] = $"-{args[count]}";
                }
            }

            Parser parser = new Parser((with) =>
            {
                with.EnableDashDash = true;
                with.CaseInsensitiveEnumValues = true;
                with.CaseSensitive = false;
                with.IgnoreUnknownArguments = true;
                with.HelpWriter = Console.Error;
            });

            ParserResult<Options> options = parser.ParseArguments<Options>(args);
            options.WithParsed<Options>(async o => await RunPlugin(o));
        }

        private static async Task RunPlugin(Options options)
        {
            AsyncTcpClient client;
            ManualResetEvent connectEvent = new ManualResetEvent(false);
            ManualResetEvent disconnectEvent = new ManualResetEvent(false);

            StreamDeckConnection connection = new StreamDeckConnection(options.Port, options.PluginUUID, options.RegisterEvent);
            Dictionary<string, JObject> settings = new Dictionary<string, JObject>();
            List<string> statusList = new List<string>();
            connection.OnConnected += (sender, args) =>
            {
                connectEvent.Set();
            };

            connection.OnDisconnected += (sender, args) =>
            {
                disconnectEvent.Set();
            };

            connection.OnApplicationDidLaunch += async (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Launch: {args.Event.Payload.Application}");
                
            };

            connection.OnApplicationDidTerminate += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Terminate: {args.Event.Payload.Application}");
            };

            connection.OnDeviceDidConnect += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Device Connected");
            };

            connection.OnDeviceDidDisconnect += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Device Disconnected");
            };

            connection.OnDidReceiveGlobalSettings += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Received Global Settings");
            };

            connection.OnDidReceiveSettings += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App Received Settings");
            };

            connection.OnKeyDown += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App KeyDown: {args.Event.Action}");
            };

            connection.OnKeyUp += async (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App KeyUp: {args.Event.Action}, {args.Event.Context}");

                await HandleKeyUp(args.Event.Action, settings, args.Event.Context);
            };

            connection.OnSendToPlugin += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("App SendToPlugin");
                
            };

            connection.OnWillAppear += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App OnWillAppear");
                connection.GetSettingsAsync(args.Event.Context);
            };

            connection.OnPropertyInspectorDidAppear += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Property Inspector Did Appear");
                connection.GetSettingsAsync(args.Event.Context);
            };

            connection.OnDidReceiveSettings += (sender, args) =>
            {
                switch (args.Event.Action)
                {
                    case "com.resnexsoft.freestyler.remote.pidemo":
                        lock (settings)
                        {
                            settings[args.Event.Context] = args.Event.Payload.Settings;
                            if (settings[args.Event.Context] == null)
                            {
                                settings[args.Event.Context] = new JObject();
                            }
                            if (settings[args.Event.Context]["selectedValue"] == null)
                            {
                                settings[args.Event.Context]["selectedValue"] = JValue.CreateString("20");
                            }
                            if (settings[args.Event.Context]["textDemoValue"] == null)
                            {
                                settings[args.Event.Context]["textDemoValue"] = JValue.CreateString("");
                            }
                        }
                        break;
                    case BaseUuid + ".togglesequence":
                    case BaseUuid + ".selectcuelisttab":
                    case BaseUuid + ".toggleoverride":
                    case BaseUuid + ".selectgroup":
                    case BaseUuid + ".blackoutgroup":
                    case BaseUuid + ".togglecuelist":
                    case BaseUuid + ".toggleoverridetab":
                        lock (settings)
                        {
                            settings[args.Event.Context] = args.Event.Payload.Settings;
                            if (settings[args.Event.Context] == null)
                            {
                                settings[args.Event.Context] = new JObject();
                            }

                            if (settings[args.Event.Context]["selectedValue"] == null)
                            {
                                settings[args.Event.Context]["selectedValue"] = JValue.CreateString("1");
                            }
                        }
                        break;
                }
            };

            connection.OnWillDisappear += (sender, args) =>
            {
                lock (settings)
                {
                    if (settings.ContainsKey(args.Event.Context))
                    {
                        settings.Remove(args.Event.Context);
                    }
                }
            };

            // Start the connection
            connection.Run();

            // Current Directory is the base Stream Deck Install path.
            // For example: C:\Program Files\Elgato\StreamDeck\

            // Wait for up to 10 seconds to connect
            if (connectEvent.WaitOne(TimeSpan.FromSeconds(10)))
            {
                // Check if Freestyler is running, and get the status of all items if so
                if (System.Diagnostics.Process.GetProcessesByName("FreestylerX2").Length > 0)
                {
                    for (int i = 1; i < 24; i++)
                    {
                        // statusList.Add(await GetStatus(i));
                    }
                }

                // We connected, loop every second until we disconnect
                while (!disconnectEvent.WaitOne(TimeSpan.FromMilliseconds(1000)))
                {

                }
            }
        }

        private static async Task SendTcpCommand(List<Tuple<string, string>> commands)
        {
            AsyncTcpClient client = new AsyncTcpClient();

            try
            {
                if (await client.ConnectAsync())
                {
                    foreach (var command in commands)
                    {
                        await client.SendAsync(command.Item1, command.Item2);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                client.Disconnect();
            }
        }
        
        private static async Task HandleKeyUp(string action, Dictionary<string, JObject> settings, string context)
        {
            
            switch (action)
            {
                case BaseUuid + ".toggleall":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("000", Toggle) });
                    break;
                
                case BaseUuid + ".togglefavorite":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("001", Toggle) });
                    break;

                case BaseUuid + ".blackout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("002", Toggle) });
                    break;

                case BaseUuid + ".freeze":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("123", Toggle) });
                    break;

                case BaseUuid + ".releaseall":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("024", Toggle) });
                    break;

                case BaseUuid + ".fog":
                    // This one is a momentary button, so it will need a short delay to turn off
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("176", On), new Tuple<string, string>("176", Off) });
                    break;

                case BaseUuid + ".foglevel":
                    // TODO:  get value 0 - 255 from prop insp
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("304", Toggle) });
                    break;

                case BaseUuid + ".fogfanspeed":
                    // TODO:  get value 0 - 255 from prop insp
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("305", Toggle) });
                    break;

                case BaseUuid + ".lockmidi":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("181", Toggle) });
                    break;

                case BaseUuid + ".master100":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("151", Toggle) });
                    break;

                case BaseUuid + ".master0":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("152", Toggle) });
                    break;

                case BaseUuid + ".fadein":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("153", Toggle) });
                    break;

                case BaseUuid + ".fadeout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("154", Toggle) });
                    break;

                case BaseUuid + ".playSequence":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("577", Toggle) });
                    break;

                case BaseUuid + ".nextscene":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("575", Toggle) });
                    break;

                case BaseUuid + ".prevscene":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("576", Toggle) });
                    break;

                case BaseUuid + ".dmx400mode":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("564", Toggle) });
                    break;

                case BaseUuid + ".dmx400blackout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("310", Toggle) });
                    break;

                case BaseUuid + ".dmx400Full":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("311", On), new Tuple<string, string>("311", Off) });
                    break;

                case BaseUuid + ".dmx400fade":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("312", Toggle) });
                    break;

                case BaseUuid + ".dmx400autochange":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("315", Toggle) });
                    break;

                case BaseUuid + ".dmx400colorchange":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("316", Toggle) });
                    break;

                case BaseUuid + ".tapsync":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("009", Toggle) });
                    break;

                case BaseUuid + ".mantrig":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("207", Toggle) });
                    break;

                case BaseUuid + ".soundtolighttrigger":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("232", Toggle) });
                    break;

                case BaseUuid + ".tapsyncdisable":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("134", Toggle) });
                    break;

                case BaseUuid + ".prevgroup":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("296", Toggle) });
                    break;

                case BaseUuid + ".nextgroup":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("297", Toggle) });
                    break;

                case BaseUuid + ".prevoverridetab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("298", Toggle) });
                    break;

                case BaseUuid + ".nextoverridetab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("299", Toggle) });
                    break;

                case BaseUuid + ".disableoverrides":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("265", Toggle) });
                    break;

                case BaseUuid + ".prevcuelisttab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("300", Toggle) });
                    break;

                case BaseUuid + ".nextcuelisttab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("301", Toggle) });
                    break;

                case BaseUuid + ".togglesequence":
                    await ToggleSequence((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".selectcuelisttab":
                    await SelectCueListTab((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".toggleoverride":
                    await ToggleOverride((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".selectgroup":
                    await SelectGroup((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".blackoutgroup":
                    await BlackoutGroup((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".togglecuelist":
                    await ToggleCueList((int)settings[context]["selectedValue"]);
                    break;

                case BaseUuid + ".toggleoverridetab":
                    await SelectOverrideTab((int)settings[context]["selectedValue"]);
                    break;

                default:
                    if (action.StartsWith(BaseUuid + ".open"))
                    {
                        string panel = action.Replace(BaseUuid + ".open", "");
                        await OpenPanel(panel);
                    }

                    break;
            }
        }

        private static async Task OpenPanel(string panel)
        {
            Dictionary<string, string> panelDict = new Dictionary<string, string>()
            {
                {"all", "569"}, {"gobo", "003"}, {"color", "004"}, {"pantilt", "005"}, {"beam", "006"}, {"macro", "007"}, {"dmx400", "008"},
                {"lamp", "010"}, {"createsequence", "011"}, {"cue", "012"}, {"sound", "013"}, {"output", "014"}, {"framing", "029"},
                {"override", "570"}, {"master", "572"}, {"submaster", "573"}, {"smoke", "574"}, {"sliders", "015"}, {"fx", "581"}, {"cuelist", "670"}
            };

            if (!panelDict.ContainsKey(panel))
            {
                return;
            }

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(panelDict[panel], Toggle) });
        }

        private static async Task ToggleSequence(int seqNumber)
        {
            if (seqNumber < 1 || seqNumber > 20)
            {
                return;
            }
        
            List<string> sequence = new List<string>() {"0", "046", "047", "048", "049", "050", "051", "052", "053", "054", "055",
                                                        "056", "057", "058", "059", "060", "061", "062", "063", "064", "065"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(sequence[seqNumber], Toggle) });
        }

        private static async Task SelectCueListTab(int cueListTab)
        {
            if (cueListTab < 1 || cueListTab > 6)
            {
                return;
            }

            List<string> cueListTabList = new List<string>() { "0", "266", "267", "268", "269", "270", "271" };

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(cueListTabList[cueListTab], Toggle) });
        }

        private static async Task ToggleCueList(int cueListNumber)
        {
            if (cueListNumber < 1 || cueListNumber > 32)
            {
                return;
            }

            List<string> cueList = new List<string>() {"0", "272", "273", "274", "275", "276", "277", "278", "279", "280", "281",
                "282", "283", "284", "285", "286", "287", "671", "672", "673", "674", "675", "676", "677", "678", "679", "680",
                "681", "682", "683", "684", "685", "686"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(cueList[cueListNumber], Toggle) });
        }

        private static async Task ToggleOverride(int overrideButton)
        {
            if (overrideButton < 1 || overrideButton > 32)
            {
                return;
            }

            List<string> overrideList = new List<string>() {"0", "066", "067", "068", "069", "070", "071", "072", "073", "074", "075",
                "076", "077", "078", "079", "080", "081", "082", "083", "084", "085", "086", "087", "088", "089", "090", "091", "092",
                "093", "094", "095", "096", "097"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(overrideList[overrideButton], On), new Tuple<string, string>(overrideList[overrideButton], Off) });
        }

        private static async Task SelectOverrideTab(int overrideTab)
        {
            if (overrideTab < 1 || overrideTab > 6)
            {
                return;
            }

            List<string> overrideTabList = new List<string>() { "0", "234", "235", "236", "237", "238", "239" };

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(overrideTabList[overrideTab], Toggle) });
        }

        private static async Task SelectGroup(int groupNumber)
        {
            if (groupNumber < 1 || groupNumber > 24)
            {
                return;
            }

            List<string> groupList = new List<string>() {"0", "034", "035", "036", "037", "038", "039", "040", "041", "042", "043",
                "550", "551", "552", "553", "554", "555", "556", "557", "558", "559", "560", "561", "562", "563"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(groupList[groupNumber], Toggle) });
        }

        private static async Task BlackoutGroup(int groupNumber)
        {
            if (groupNumber < 1 || groupNumber > 24)
            {
                return;
            }

            List<string> groupList = new List<string>() {"0", "098", "099", "100", "101", "102", "103", "104", "105", "106", "107",
                                                            "108", "109", "110", "111", "112", "113", "114", "115", "116", "117", "118", "119", "120", "121"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(groupList[groupNumber], Toggle) });
        }

        private static async Task<string> GetStatus(int item)
        {
            string resp = "";
            List<string> itemList = new List<string>()
            {
                "0", "001", "002", "003", "004", "005", "006", "007", "008", "009", "010",
                "011", "012", "013", "014", "015", "016", "017", "018", "019", "020", "021",
                "022", "023"
            };

            AsyncTcpClient client = new AsyncTcpClient();

            try
            {
                if (await client.ConnectAsync())
                {
                    resp = await client.QueryAsync(itemList[item]);
                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
            finally
            {
                client.Disconnect();
            }

            return resp;
        }
    }
}