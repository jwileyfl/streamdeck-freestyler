using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Lifetime;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace FreestylerRemote
{
    internal class TCPClient
    {
        private const string FreestylerIp = "127.0.0.1";
        private const int FreestylerPort = 3332;
        private readonly IPEndPoint _serverEndPoint = new IPEndPoint(IPAddress.Parse(FreestylerIp), FreestylerPort); 
        private TcpClient _client;
        private NetworkStream _clientStream;
        private StreamWriter _writer;

        public enum Protocol
        {
            Byte = 0,
            Ascii = 1,
            StatusByte = 3
        }

        public TCPClient()
        {
            _client = new TcpClient();
        }

        ~TCPClient()
        {
            if (_client != null)
            {
                Disconnect();
            }
        }

        internal void Connect()
        {
            try
            {
                _client.Connect(_serverEndPoint);
                _clientStream = _client.GetStream();
                _clientStream.ReadTimeout = 2000;
                _writer = new StreamWriter(_clientStream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        internal void Send(byte cmd1, byte cmd2)
        {
            const int click = 255;
            const int release = 0;

            //F,S,O,D,Blackout,Spare,Click,Spare,Spare
            byte[] buffer = { 70, 83, 79, 68, cmd1, cmd2, click, 0, 0 };

            //F,S,O,D,Blackout,Spare,Release,Spare,Spare
            byte[] buffer2 = { 70, 83, 79, 68, cmd1, cmd2, release, 0, 0 };

            _clientStream.Write(buffer, 0, buffer.Length);
            _clientStream.Flush();

            _clientStream.Write(buffer2, 0, buffer2.Length);
            _clientStream.Flush();
            
        }

        internal void Send(string code, string arg, string opt="")
        {

            //F,S,O,C,Code ("xxx"),TCP/IP Arg (Click/Release or Fader Value), Optional ("zzz")
            string buffer = "FSOC" + code + arg; // + opt;

                _writer.Write(buffer);
                _writer.Flush();
        }

        internal string Query(string code)
        {
            code = "FSBC" + code + "000";
            byte[] respBuffer = new byte[1024];
            string resp = "";
            int counter = 0;

            //F, S, B, C, #, #, #, 0, 0, 0
            //byte[] buffer = { 70, 83, 66, 67, cmd1, cmd2, cmd3, 0, 0, 0};
            byte[] buffer = Encoding.ASCII.GetBytes(code);
            _clientStream.Write(buffer, 0, buffer.Length);
            _clientStream.Flush();

            do
            {
                System.Threading.Thread.Sleep(500);
                counter++;
            } while (!_clientStream.DataAvailable && counter < 5);

            try
            {
                if (_clientStream.DataAvailable)
                {
                    int numBytes = _clientStream.Read(respBuffer, 0, respBuffer.Length);
                    resp = Encoding.ASCII.GetString(respBuffer, 0, numBytes);
                    resp = resp.Trim('?');
                    resp = resp.Replace("FSBC", "");
                    resp = resp.Trim();
                    
                    if (Convert.ToInt32(resp[0]) < 33 || Convert.ToInt32(resp[0]) > 126)
                    {
                        resp = resp.Remove(0, 1);
                    }

                    if (resp.StartsWith(","))
                    {
                        resp = resp.Remove(0, 1);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return resp;
        }

        internal void Disconnect()
        {
            if (_writer != null)
            {
                _writer.Close();
                _writer = null;
            }

            if (_clientStream != null)
            {
                _clientStream.Close();
                _clientStream = null;
            }

            if (_client != null)
            {
                _client.Close();
                _client = null;
            }

        }
    }
}
