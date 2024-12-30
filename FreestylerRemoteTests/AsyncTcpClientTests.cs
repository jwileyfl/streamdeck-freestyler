// <copyright file="AsyncTcpClientTests.cs" company="Resnexsoft">
//     Copyright (c) Resnexsoft. All rights reserved.
// </copyright>
// <author>Jeremy Wiley</author>

namespace FreestylerRemoteTests
{
    using System;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class AsyncTcpClientTests : IDisposable
    {
        private readonly FreestylerRemote.AsyncTcpClient client;
        private bool FreestylerIsRunning = false;

        private Mock<NetworkStream> mockStream;
        private Mock<TcpClient> mockClient;

        public AsyncTcpClientTests()
        {
            this.mockStream = new Mock<NetworkStream>();
            this.mockClient = new Mock<TcpClient>();

            this.client = new FreestylerRemote.AsyncTcpClient();
        }

        public void Dispose()
        {
            this.client.Dispose();
        }

        [TestInitialize]
        public void Initialize()
        {
            this.FreestylerIsRunning = FreestylerRemote.Program.IsFreestylerRunning();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.client.Disconnect();
        }

        [TestMethod]
        public async Task ConnectAsync_NoFreestylerRunning_Fail()
        {
            
            if (this.FreestylerIsRunning)
            {
                Assert.Inconclusive("Freestyler is running, cannot test this scenario.");
            }

            bool success = await this.client.ConnectAsync();

            Assert.IsFalse(success);
        }

        [TestMethod]
        public async Task ConnectAsync_FreestylerRunning_Success()
        {
            if (!this.FreestylerIsRunning)
            {
                Assert.Inconclusive("Freestyler is not running, cannot test this scenario.");
            }

            bool success = await this.client.ConnectAsync();

            Assert.IsTrue(success);
        }

        [TestMethod]
        public async Task SendAsync_ToggleAll_Pass()
        {
            if (!this.FreestylerIsRunning)
            {
                Assert.Inconclusive("Freestyler is not running, cannot test this scenario.");
            }

            bool success = await this.client.ConnectAsync();

            Assert.IsTrue(success);

            Assert.IsInstanceOfType(this.client.SendAsync("000", "255"), typeof(Task));
        }

        [TestMethod]
        public async Task QueryAsync_GetMasterIntensity_Pass()
        {
            if (!this.FreestylerIsRunning)
            {
                Assert.Inconclusive("Freestyler is not running, cannot test this scenario.");
            }

            bool success = await this.client.ConnectAsync();

            Assert.IsTrue(success);

            string response = await this.client.QueryAsync(FreestylerRemote.Program.FreestylerStatus["masterIntensity"].ToString("000"));
            
            Assert.IsNotNull(response);
            Assert.IsTrue(response.Length > 0);
            Assert.IsTrue(int.TryParse(response, out int _));
        }
    }
}
