using CommandLine;
using CommandLine.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using streamdeck_client_csharp;
using streamdeck_client_csharp.Events;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FreestylerRemote
{
    internal class Program
    {
        private const string BaseUuid = "com.resnexsoft.freestyler.remote";
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
        static void Main(string[] args)
        {
            // Uncomment this line of code to allow for debugging
            //while (!System.Diagnostics.Debugger.IsAttached) { System.Threading.Thread.Sleep(100); }

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
            options.WithParsed<Options>(o => RunPlugin(o));
        }

        static void RunPlugin(Options options)
        {
            TCPClient client;
            ManualResetEvent connectEvent = new ManualResetEvent(false);
            ManualResetEvent disconnectEvent = new ManualResetEvent(false);

            StreamDeckConnection connection = new StreamDeckConnection(options.Port, options.PluginUUID, options.RegisterEvent);

            connection.OnConnected += (sender, args) =>
            {
                connectEvent.Set();
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

            connection.OnKeyDown += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App KeyDown: " + args.Event.Action);
            };

            connection.OnKeyUp += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App KeyUp: " + args.Event.Action);

                switch (args.Event.Action)
                {
                    case BaseUuid + ".blackout":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("002", "255");
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
                        
                        break;
                    case BaseUuid + ".master100":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("151", "255");
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

                        break;
                    case BaseUuid + ".master0":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("152", "255");
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

                        break;
                    case BaseUuid + ".fadein":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("153", "255");
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

                        break;
                    case BaseUuid + ".fadeout":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("154", "255");
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

                        break;
                    case BaseUuid + ".playSequence":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("577", "255");
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
                        break;
                    case BaseUuid + ".nextscene":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("575", "255");
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
                        break;
                    case BaseUuid + ".dmx400blackout":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("310", "255");
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
                        break;
                    case BaseUuid + ".dmx400Full":
                        client = new TCPClient();
                        try
                        {
                            client.Connect();
                            client.Send("311", "255");
                            client.Send("311","000");
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
                        break;
                    default:
                        if (args.Event.Action.StartsWith(BaseUuid + ".togglesequence"))
                        {
                            int num = Int32.Parse(args.Event.Action.Last().ToString());
                            ToggleSequence(num);
                        }
                        else if (args.Event.Action.StartsWith(BaseUuid + ".togglecuelist"))
                        {
                            int num = Int32.Parse(args.Event.Action.Last().ToString());
                            ToggleCueList(num);
                        }
                        else if (args.Event.Action.StartsWith(BaseUuid + ".override"))
                        {
                            int num = Int32.Parse(args.Event.Action.Last().ToString());
                            ToggleOverride(num);
                        }
                        else if (args.Event.Action.StartsWith(BaseUuid + ".group"))
                        {
                            int num = Int32.Parse(args.Event.Action.Last().ToString());
                            SelectGroup(num);
                        }
                        break;
                }
            };

            connection.OnSendToPlugin += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App SendToPlugin");
            };

            Dictionary<string, JObject> settings = new Dictionary<string, JObject>();
            
            connection.OnWillAppear += (sender, args) =>
            {
                System.Diagnostics.Debug.WriteLine($"App OnWillAppear");

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
                // We connected, loop every second until we disconnect
                while (!disconnectEvent.WaitOne(TimeSpan.FromMilliseconds(1000)))
                {
                    
                }
            }
        }

        static void ToggleSequence(int seqNumber)
        {
            if (seqNumber < 1 || seqNumber > 20)
            {
                return;
            }
        
            List<string> sequence = new List<string>() {"0", "046", "047", "048", "049", "050", "051", "052", "053", "054", "055",
                                                        "056", "057", "058", "059", "060", "061", "062", "063", "064", "065"};
            TCPClient client = new TCPClient();

            try
            {
                client.Connect();
                client.Send(sequence[seqNumber], "255");
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

        static void ToggleCueList(int cueListNumber)
        {
            if (cueListNumber < 1 || cueListNumber > 32)
            {
                return;
            }

            List<string> cueList = new List<string>() {"0", "272", "273", "274", "275", "276", "277", "278", "279", "280", "281",
                "282", "283", "284", "285", "286", "287", "671", "672", "673", "674", "675", "676", "677", "678", "679", "680",
                "681", "682", "683", "684", "685", "686"};
            TCPClient client = new TCPClient();

            try
            {
                client.Connect();
                client.Send(cueList[cueListNumber], "255");
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

        static void ToggleOverride(int overrideButton)
        {
            if (overrideButton < 1 || overrideButton > 32)
            {
                return;
            }

            List<string> overrideList = new List<string>() {"0", "066", "067", "068", "069", "070", "071", "072", "073", "074", "075",
                "076", "077", "078", "079", "080", "081", "082", "083", "084", "085", "086", "087", "088", "089", "090", "091", "092",
                "093", "094", "095", "096", "097"};
            TCPClient client = new TCPClient();

            try
            {
                client.Connect();
                client.Send(overrideList[overrideButton], "255");
                client.Send(overrideList[overrideButton], "000");
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

        static void SelectGroup(int groupNumber)
        {
            if (groupNumber < 1 || groupNumber > 24)
            {
                return;
            }

            List<string> groupList = new List<string>() {"0", "034", "035", "036", "037", "038", "039", "040", "041", "042", "043",
                "550", "551", "552", "553", "554", "555", "556", "557", "558", "559", "560", "561", "562", "563"};
            TCPClient client = new TCPClient();

            try
            {
                client.Connect();
                client.Send(groupList[groupNumber], "255");
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
    }
}