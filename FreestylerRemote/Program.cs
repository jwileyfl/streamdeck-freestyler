// <copyright file="Program.cs" company="Resnexsoft">
//     Resnexsoft. All rights reserved.
// </copyright>
// <author>Jeremy Wiley</author>

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
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Remoting.Channels;
    using System.Runtime.Remoting.Contexts;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Program class for the Freestyler Remote StreamDeck Plugin
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The base UUID for the plugin
        /// </summary>
        private const string BaseUuid = "com.resnexsoft.freestyler.remote";

        /// <summary>
        /// Button On value for freestyler
        /// </summary>
        private const string On = "255";

        /// <summary>
        /// Button Off value for freestyler
        /// </summary>
        private const string Off = "000";

        /// <summary>
        /// Button Toggle value for freestyler
        /// </summary>
        private const string Toggle = "255";

        /// <summary>
        /// FreeStyler status values available for request
        /// </summary>
        internal static readonly Dictionary<string, int> FreestylerStatus = new Dictionary<string, int>() 
        {
            { "cueCaptions", 1 }, { "overrideCaptions", 2 }, { "cuelistCaptionsCurrent", 3 }, { "cuelistCaptionsAll", 4 }, { "cueStatus", 5 },
            { "overrideButtonsSetting", 6 }, { "overrideButtonsStatus", 7 }, { "groupNames", 8 }, { "groupStatus", 9 }, { "version", 10 },
            { "submasterNames", 11 }, { "submasterStatus", 12 }, { "submasterIntensity", 13 }, { "blkoutFreezeFavStatus", 14 }, { "cueCurrent", 15 },
            { "cuelistStatus", 16 }, { "fixtureNames", 17 }, { "fixtureAddress", 18 }, { "cueSpeed", 19 }, { "masterCueSpeed", 20 }, 
            { "masterIntensity", 21 }, { "fogFanLevels", 22 }, { "fixtureSelected", 23 }
        };

        private static readonly SemaphoreSlim SlimSem = new SemaphoreSlim(1, 1);
        private static bool isQueryRunning = false;
        private static string cueCaptions = "";
        private static string overrideCaptions = "";
        private static string cuelistCaptionsCurrent = "";
        private static string cuelistCaptionsAll = "";
        private static string cueStatus = "";
        private static string overrideButtonsSetting = "";
        private static string overrideButtonsStatus = "";
        private static string groupNames = "";
        private static string groupStatus = "";
        private static string version = "";
        private static string submasterNames = "";
        private static string submasterStatus = "";
        private static string submasterIntensity = "";
        private static string blkoutFreezeFavStatus = "";
        private static string cueCurrent = "";
        private static string cuelistStatus = "";
        private static string fixtureNames = "";
        private static string fixtureAddress = "";
        private static string cueSpeed = "";
        private static string masterCueSpeed = "";
        private static string masterIntensity = "";
        private static string fogFanLevels = "";
        private static string fixtureSelected = "";

        private static Dictionary<string, int> intensityDisplays = new Dictionary<string, int>();

        // StreamDeck launches the plugin with these details
        // -port [number] -pluginUUID [GUID] -registerEvent [string?] -info [json]

        /// <summary>
        /// Main entry point for the application
        /// </summary>
        /// <param name="args">StreamDeck Plugin Arguments</param>
        private static void Main(string[] args)
        {

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

        /// <summary>
        /// Run the StreamDeck plugin
        /// </summary>
        /// <param name="options">StreamDeck Plugin Options</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task RunPlugin(Options options, CancellationToken cancellationToken = default)
        {
            ManualResetEvent connectEvent = new ManualResetEvent(false);
            ManualResetEvent disconnectEvent = new ManualResetEvent(false);

            StreamDeckConnection connection = new StreamDeckConnection(options.Port, options.PluginUUID, options.RegisterEvent);
            Dictionary<string, JObject> settings = new Dictionary<string, JObject>();
            List<string> statusList = new List<string>();
            string tmpValue = string.Empty;

            connection.OnConnected += async (sender, args) =>
            {
                connectEvent.Set();

                if (IsFreestylerRunning())
                {
                    masterIntensity = await GetStatus(FreestylerStatus["masterIntensity"], cancellationToken);
                    tmpValue = masterIntensity;
                }
            };

            connection.OnDisconnected += (sender, args) =>
            {
                disconnectEvent.Set();
            };

            connection.OnApplicationDidLaunch += (sender, args) =>
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

                await HandleKeyUp(args.Event.Action, settings, args.Event.Context, cancellationToken);
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

            connection.OnPropertyInspectorDidDisappear += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Property Inspector Did Disappear");
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

                    case BaseUuid + ".displaymasterintensity":
                        lock (intensityDisplays)
                        {
                            intensityDisplays[args.Event.Context] = 0;
                        }
                        break;
                    case BaseUuid + ".togglesequence":
                    case BaseUuid + ".selectcuelisttab":
                    case BaseUuid + ".toggleoverride":
                    case BaseUuid + ".selectgroup":
                    case BaseUuid + ".blackoutgroup":
                    case BaseUuid + ".togglecuelist":
                    case BaseUuid + ".toggleoverridetab":
                    case BaseUuid + ".foglevel":
                    case BaseUuid + ".fogfanspeed":
                    case BaseUuid + ".masterintensity":
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

            connection.OnTitleParametersDidChange += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine("Title Parameters Did Change");
            };

            // Start the connection
            connection.Run();

            // Current Directory is the base Stream Deck Install path.
            // For example: C:\Program Files\Elgato\StreamDeck\

            // Wait for up to 10 seconds to connect
            if (connectEvent.WaitOne(TimeSpan.FromSeconds(10)))
            {
                // We connected, loop every second until we disconnect
                while (!disconnectEvent.WaitOne(TimeSpan.FromMilliseconds(500)))
                {
                    if (IsFreestylerRunning())
                    {
                        tmpValue = GetStatus(FreestylerStatus["masterIntensity"]).Result;
                    }

                    if (masterIntensity != tmpValue)
                    {
                        masterIntensity = tmpValue;

                        lock (intensityDisplays)
                        {
                            foreach (KeyValuePair<string, int> kvp in intensityDisplays.ToArray())
                            {
                                connection.SetTitleAsync($"{masterIntensity}%", kvp.Key, SDKTarget.HardwareAndSoftware, null);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sends command(s) to the Freestyler TCP server
        /// </summary>
        /// <param name="commands">List of command strings to be sent to Freestyler</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        /// <example> <code> await SendTcpCommand( new List&lt;Tuple&lt;string, string&gt;&gt;() { new Tuple&lt;string, string&gt;("000", Toggle) }); </code> </example>
        private static async Task SendTcpCommand(List<Tuple<string, string>> commands, CancellationToken cancellationToken = default)
        {
            AsyncTcpClient client = new AsyncTcpClient();

            try
            {
                if (await client.ConnectAsync(cancellationToken))
                {
                    foreach (var command in commands)
                    {
                        await client.SendAsync(command.Item1, command.Item2, string.Empty, cancellationToken);
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

        /// <summary>
        /// Handle the KeyUp event from the StreamDeck
        /// </summary>
        /// <param name="action">Event Action (UUID as defined in manifest.json)</param>
        /// <param name="settings">Settings collected from action property inspector</param>
        /// <param name="context">Event Context</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task HandleKeyUp(string action, Dictionary<string, JObject> settings, string context, CancellationToken cancellationToken = default)
        {
            switch (action)
            {
                case BaseUuid + ".toggleall":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("000", Toggle) }, cancellationToken);
                    break;
                
                case BaseUuid + ".togglefavorite":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("001", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".blackout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("002", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".freeze":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("123", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".releaseall":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("024", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".fog":
                    // This one is a momentary button, so it will need a short delay to turn off
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("176", On), new Tuple<string, string>("176", Off) }, cancellationToken);
                    break;

                case BaseUuid + ".foglevel":
                    await SetFogLevel((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".fogfanspeed":
                    await SetFogFanSpeed((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".lockmidi":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("181", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".master100":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("151", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".master0":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("152", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".masterintensity":
                    await SetMasterIntensity((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".fadein":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("153", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".fadeout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("154", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".playSequence":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("577", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".nextscene":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("575", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".prevscene":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("576", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400mode":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("564", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400blackout":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("310", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400Full":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("311", On), new Tuple<string, string>("311", Off) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400fade":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("312", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400autochange":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("315", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".dmx400colorchange":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("316", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".tapsync":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("009", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".mantrig":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("207", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".soundtolighttrigger":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("232", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".tapsyncdisable":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("134", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".prevgroup":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("296", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".nextgroup":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("297", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".prevoverridetab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("298", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".nextoverridetab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("299", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".disableoverrides":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("265", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".prevcuelisttab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("300", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".nextcuelisttab":
                    await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("301", Toggle) }, cancellationToken);
                    break;

                case BaseUuid + ".togglesequence":
                    await ToggleSequence((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".selectcuelisttab":
                    await SelectCueListTab((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".toggleoverride":
                    await ToggleOverride((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".selectgroup":
                    await SelectGroup((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".blackoutgroup":
                    await BlackoutGroup((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".togglecuelist":
                    await ToggleCueList((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                case BaseUuid + ".toggleoverridetab":
                    await SelectOverrideTab((int)settings[context]["selectedValue"], cancellationToken);
                    break;

                default:
                    if (action.StartsWith(BaseUuid + ".open"))
                    {
                        string panel = action.Replace(BaseUuid + ".open", "");
                        await OpenPanel(panel, cancellationToken);
                    }

                    break;
            }
        }

        /// <summary>
        /// Open a panel in Freestyler
        /// </summary>
        /// <param name="panel">panel to open</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task OpenPanel(string panel, CancellationToken cancellationToken = default)
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

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(panelDict[panel], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Toggle a sequence in Freestyler
        /// </summary>
        /// <param name="seqNumber">sequence to toggle (1 to 20)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task ToggleSequence(int seqNumber, CancellationToken cancellationToken = default)
        {
            if (seqNumber < 1 || seqNumber > 20)
            {
                return;
            }
        
            List<string> sequence = new List<string>() {"0", "046", "047", "048", "049", "050", "051", "052", "053", "054", "055",
                                                        "056", "057", "058", "059", "060", "061", "062", "063", "064", "065"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(sequence[seqNumber], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Select a tab from the cue list panel in Freestyler
        /// </summary>
        /// <param name="cueListTab">cue list tab to select (1 to 6)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SelectCueListTab(int cueListTab, CancellationToken cancellationToken = default)
        {
            if (cueListTab < 1 || cueListTab > 6)
            {
                return;
            }

            List<string> cueListTabList = new List<string>() { "0", "266", "267", "268", "269", "270", "271" };

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(cueListTabList[cueListTab], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Toggle a cue list in Freestyler
        /// </summary>
        /// <param name="cueListNumber">cue list to toggle (1 to 32)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task ToggleCueList(int cueListNumber, CancellationToken cancellationToken = default)
        {
            if (cueListNumber < 1 || cueListNumber > 32)
            {
                return;
            }

            List<string> cueList = new List<string>() {"0", "272", "273", "274", "275", "276", "277", "278", "279", "280", "281",
                "282", "283", "284", "285", "286", "287", "671", "672", "673", "674", "675", "676", "677", "678", "679", "680",
                "681", "682", "683", "684", "685", "686"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(cueList[cueListNumber], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Toggle a button on the current override panel tab in Freestyler
        /// </summary>
        /// <param name="overrideButton">override button to toggle (1 to 32)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task ToggleOverride(int overrideButton, CancellationToken cancellationToken = default)
        {
            if (overrideButton < 1 || overrideButton > 32)
            {
                return;
            }

            List<string> overrideList = new List<string>() {"0", "066", "067", "068", "069", "070", "071", "072", "073", "074", "075",
                "076", "077", "078", "079", "080", "081", "082", "083", "084", "085", "086", "087", "088", "089", "090", "091", "092",
                "093", "094", "095", "096", "097"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(overrideList[overrideButton], On), new Tuple<string, string>(overrideList[overrideButton], Off) }, cancellationToken);
        }

        /// <summary>
        /// Select a tab from the override panel in Freestyler
        /// </summary>
        /// <param name="overrideTab">override tab to select (1 to 6)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SelectOverrideTab(int overrideTab, CancellationToken cancellationToken = default)
        {
            if (overrideTab < 1 || overrideTab > 6)
            {
                return;
            }

            List<string> overrideTabList = new List<string>() { "0", "234", "235", "236", "237", "238", "239" };

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(overrideTabList[overrideTab], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Select a fixture group in Freestyler
        /// </summary>
        /// <param name="groupNumber">fixture group to select (1 to 24)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SelectGroup(int groupNumber, CancellationToken cancellationToken = default)
        {
            if (groupNumber < 1 || groupNumber > 24)
            {
                return;
            }

            List<string> groupList = new List<string>() {"0", "034", "035", "036", "037", "038", "039", "040", "041", "042", "043",
                "550", "551", "552", "553", "554", "555", "556", "557", "558", "559", "560", "561", "562", "563"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(groupList[groupNumber], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Blackout a fixture group in Freestyler
        /// </summary>
        /// <param name="groupNumber">fixture group to blackout (1 to 24)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task BlackoutGroup(int groupNumber, CancellationToken cancellationToken = default)
        {
            if (groupNumber < 1 || groupNumber > 24)
            {
                return;
            }

            List<string> groupList = new List<string>() {"0", "098", "099", "100", "101", "102", "103", "104", "105", "106", "107",
                                                            "108", "109", "110", "111", "112", "113", "114", "115", "116", "117", "118", "119", "120", "121"};

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>(groupList[groupNumber], Toggle) }, cancellationToken);
        }

        /// <summary>
        /// Set the smoke/fog level in Freestyler
        /// </summary>
        /// <param name="level">fog level (0 to 255)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SetFogLevel(int level, CancellationToken cancellationToken = default)
        {
            if (level < 0 || level > 255)
            {
                return;
            }

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("304", level.ToString("000")) }, cancellationToken);
        }

        /// <summary>
        /// Set the smoke/fog fan speed in Freestyler
        /// </summary>
        /// <param name="level">fan speed (0 to 255)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SetFogFanSpeed(int level, CancellationToken cancellationToken = default)
        {
            if (level < 0 || level > 255)
            {
                return;
            }

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("305", level.ToString("000")) }, cancellationToken);
        }

        /// <summary>
        /// Set the master intensity in Freestyler
        /// </summary>
        /// <param name="level">intensity level to set (0 to 255)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task SetMasterIntensity(int level, CancellationToken cancellationToken = default)
        {
            if (level < 0 || level > 255)
            {
                return;
            }

            await SendTcpCommand(new List<Tuple<string, string>>() { new Tuple<string, string>("155", level.ToString("000")) }, cancellationToken);
        }

        /// <summary>
        /// Get the status of a Freestyler item
        /// </summary>
        /// <param name="item">Freestyler item to query status of</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns><see cref="Task"/></returns>
        private static async Task<string> GetStatus(int item, CancellationToken cancellationToken = default)
        {
            await SlimSem.WaitAsync(cancellationToken);

            string resp = string.Empty;
            
            while (isQueryRunning)
            {
                await Task.Delay(500, cancellationToken);
            }

            isQueryRunning = true;

            AsyncTcpClient client = new AsyncTcpClient();

            try
            {
                if (await client.ConnectAsync(cancellationToken))
                {
                    resp = await client.QueryAsync(item.ToString("000"), cancellationToken);
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
                isQueryRunning = false;
                SlimSem.Release();
            }

            return resp;
        }

        /// <summary>
        /// Check for an instance of Freestyler
        /// </summary>
        /// <returns><see cref="bool"/></returns>
        internal static bool IsFreestylerRunning()
        {
            return System.Diagnostics.Process.GetProcessesByName("FreestylerX2").Length > 0;
        }

        /// <summary>
        /// StreamDeck Plugin <c>Options</c> class
        /// <list type="bullet">
        /// <item>
        /// <term>Port</term>
        /// <description>The websocket port to connect to</description>
        /// </item>
        /// <item>
        /// <term>PluginUUID</term>
        /// <description>The UUID of the plugin</description>
        /// </item>
        /// <item>
        /// <term>RegisterEvent</term>
        /// <description>The event triggered when the plugin is registered</description>
        /// </item>
        /// <item>
        /// <term>Info</term>
        /// <description>Extra JSON launch data</description>
        /// </item>
        /// </list>
        /// </summary>
        /// <remarks>
        /// These options are passed to the plugin when it is launched by the StreamDeck application
        /// </remarks>
        public class Options
        {
            /// <summary>
            /// Gets or sets the Port to connect to
            /// </summary>
            [Option("port", Required = true, HelpText = "The websocket port to connect to", SetName = "port")]
            public int Port { get; set; }

            /// <summary>
            /// Gets or sets the UUID of the plugin
            /// </summary>
            [Option("pluginUUID", Required = true, HelpText = "The UUID of the plugin")]
            public string PluginUUID { get; set; }

            /// <summary>
            /// Gets or sets the event triggered when the plugin is registered
            /// </summary>
            [Option("registerEvent", Required = true, HelpText = "The event triggered when the plugin is registered?")]
            public string RegisterEvent { get; set; }

            /// <summary>
            /// Gets or sets the extra JSON launch data
            /// </summary>
            [Option("info", Required = true, HelpText = "Extra JSON launch data")]
            public string Info { get; set; }
        }
    }
}