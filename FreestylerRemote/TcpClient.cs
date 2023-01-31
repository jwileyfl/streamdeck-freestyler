using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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
