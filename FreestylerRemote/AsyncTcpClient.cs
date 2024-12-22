namespace FreestylerRemote
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    internal class AsyncTcpClient
    {
        private const string FreestylerIp = "127.0.0.1";
        private const int FreestylerPort = 3332;
        //private readonly IPEndPoint _serverEndPoint = new IPEndPoint(IPAddress.Parse(FreestylerIp), FreestylerPort);
        private TcpClient _client;
        private NetworkStream _clientStream;
        //private StreamWriter _writer;

        public AsyncTcpClient()
        {
            _client = new TcpClient();
        }

        ~AsyncTcpClient()
        {
            if (_client != null)
            {
                Disconnect();
            }
        }

        public async Task<bool> ConnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await this._client.ConnectAsync(FreestylerIp, FreestylerPort);
                
                if (!this._client.Connected)
                {
                    return false;
                }

                this._clientStream = this._client.GetStream();
                this._clientStream.ReadTimeout = 2000;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return true;
        }

        public void Disconnect(CancellationToken cancellationToken = default)
        {
            try
            {
                if (!(this._clientStream is null))
                {
                    this._clientStream.Dispose();
                }
                
                if (!(this._client is null))
                {
                    this._client.Dispose();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task SendAsync(byte cmd1, byte cmd2, CancellationToken cancellationToken = default)
        {
            if (this._clientStream is null)
            {
                // throw new InvalidOperationException("No active connection to send data.");
                return;
            }

            const int click = 255;
            const int release = 0;

            //F,S,O,D,Blackout,Spare,Click,Spare,Spare
            byte[] buffer = { 70, 83, 79, 68, cmd1, cmd2, click, 0, 0 };

            //F,S,O,D,Blackout,Spare,Release,Spare,Spare
            byte[] buffer2 = { 70, 83, 79, 68, cmd1, cmd2, release, 0, 0 };

            await this._clientStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
            await this._clientStream.FlushAsync(cancellationToken);

            await this._clientStream.WriteAsync(buffer2, 0, buffer2.Length, cancellationToken);
            await this._clientStream.FlushAsync(cancellationToken);
        }

        public async Task SendAsync(string code, string arg, string option = "", CancellationToken cancellationToken = default)
        {
            if (this._clientStream is null)
            {
                // throw new InvalidOperationException("No active connection to send data.");
                return;
            }

            try
            {
                // F,S,O,C,Code ("xxx"),TCP/IP Arg (Click/Release or Fader Value), Optional ("zzz")
                string buffer = "FSOC" + code + arg; // + opt;

                byte[] data = Encoding.ASCII.GetBytes(buffer);
                await this._clientStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public async Task<string> ReceiveAsync(CancellationToken cancellationToken = default)
        {
            byte[] respBuffer = new byte[1024];
            string resp = "";
            int counter = 0;

            if (this._clientStream is null)
            {
                // throw new InvalidOperationException("No active connection to receive data.");
                return resp;
            }

            do
            {
                System.Threading.Thread.Sleep(500);
                counter++;
            }
            while (!this._clientStream.DataAvailable && counter < 10);

            try
            {
                if (this._clientStream.DataAvailable)
                {
                    int numBytes = await this._clientStream.ReadAsync(respBuffer, 0, respBuffer.Length, cancellationToken);

                    if (numBytes == 0)
                    {
                        return resp;
                    }

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

        public async Task<string> QueryAsync(string code, CancellationToken cancellationToken = default)
        {
            code = "FSBC" + code + "000";
            byte[] respBuffer = new byte[1024];
            string resp = "";
            int counter = 0;

            // F, S, B, C, #, #, #, 0, 0, 0
            // byte[] buffer = { 70, 83, 66, 67, cmd1, cmd2, cmd3, 0, 0, 0};
            byte[] buffer = Encoding.ASCII.GetBytes(code);

            try
            {
                await this._clientStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                await this._clientStream.FlushAsync(cancellationToken);

                do
                {
                    await Task.Delay(500, cancellationToken);
                    counter++;
                }
                while (!this._clientStream.DataAvailable && counter < 10);

                if (this._clientStream.DataAvailable)
                {
                    int numBytes = await this._clientStream.ReadAsync(respBuffer, 0, respBuffer.Length, cancellationToken);

                    // Need to look into this further to properly handle the response
                    resp = Encoding.ASCII.GetString(respBuffer, 0, numBytes);
                    resp = resp.Trim('?');
                    resp = resp.Replace("FSBC", "");
                    resp = resp.Trim();

                    if (int.TryParse(resp, out int result) && result < 33 || result > 126)
                    {
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

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return resp;
        }
    }
}
