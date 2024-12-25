// <copyright file="AsyncTcpClient.cs" company="Resnexsoft">
//     Copyright (c) Resnexsoft. All rights reserved.
// </copyright>
// <author>Jeremy Wiley</author>

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

    /// <summary>
    /// <c>AsyncTcpClient</c> class
    /// </summary>
    /// <remarks>
    /// Asynchronous TCP Client used to communicate with Freestyler over TCP/IP.
    /// </remarks>
    internal class AsyncTcpClient
    {
        /// <summary>
        /// IP Address for Freestyler instance on local machine
        /// </summary>
        private const string FreestylerIp = "127.0.0.1";

        /// <summary>
        /// TCP Port for Freestyler instance
        /// </summary>
        private const int FreestylerPort = 3332;

        /// <summary>
        /// TCP Client instance
        /// </summary>
        private TcpClient _client;

        /// <summary>
        /// Network Stream instance used to read/write data
        /// </summary>
        private NetworkStream _clientStream;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncTcpClient"/> class.
        /// </summary>
        public AsyncTcpClient()
        {
            this._client = new TcpClient();
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="AsyncTcpClient"/> class.
        /// </summary>
        ~AsyncTcpClient()
        {
            if (this._client != null)
            {
                this.Disconnect();
            }
        }

        /// <summary>
        /// Connect to Freestyler instance
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns><see cref="Task"/></returns>
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

        /// <summary>
        /// Disconnect from Freestyler instance
        /// </summary>
        public void Disconnect()
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

        /// <summary>
        /// Send byte data to Freestyler instance
        /// </summary>
        /// <param name="cmd1">command 1</param>
        /// <param name="cmd2">command 2</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns><see cref="Task"/></returns>
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

        /// <summary>
        /// Send string data to Freestyler instance
        /// </summary>
        /// <param name="code">3 character code (as specified in Freestyler docs)</param>
        /// <param name="arg">3 character TCP/IP argument (as specified in Freestyler docs)</param>
        /// <param name="option">optional (for later use)</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns><see cref="Task"/></returns>
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

        /// <summary>
        /// Receive data from Freestyler instance
        /// </summary>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns><see cref="Task"/> string response</returns>
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

        /// <summary>
        /// Query Freestyler instance for data
        /// </summary>
        /// <param name="code">3 character code</param>
        /// <param name="cancellationToken">cancellation token</param>
        /// <returns><see cref="Task"/></returns>
        public async Task<string> QueryAsync(string code, CancellationToken cancellationToken = default)
        {
            code = "FSBC" + code + "000";
            byte[] respBuffer = new byte[4096];
            string resp = string.Empty;
            int counter = 0;

            // F, S, B, C, #, #, #, 0, 0, 0
            // byte[] buffer = { 70, 83, 66, 67, cmd1, cmd2, cmd3, 0, 0, 0};
            byte[] buffer = Encoding.ASCII.GetBytes(code);

            try
            {
                await this._clientStream.WriteAsync(buffer, 0, buffer.Length, cancellationToken);
                await this._clientStream.FlushAsync(cancellationToken);

                while (!this._clientStream.DataAvailable && counter < 10)
                {
                    await Task.Delay(500, cancellationToken);
                    counter++;
                }
                
                if (this._clientStream.DataAvailable)
                {
                    int numBytes = await this._clientStream.ReadAsync(respBuffer, 0, respBuffer.Length, cancellationToken);

                    // Need to look into this further to properly handle the response
                    resp = Encoding.ASCII.GetString(respBuffer, 0, numBytes);
                    resp = resp.Trim('?');
                    resp = resp.Replace("FSBC", "");
                    resp = resp.Trim();
                    resp = new string(resp.Where(c => !char.IsControl(c)).ToArray());
                }
            }
            catch (TaskCanceledException e)
            {
                Console.WriteLine("Query Canceled");
                throw;
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
