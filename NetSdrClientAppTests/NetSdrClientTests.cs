using System;
using System.Threading.Tasks;
using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;
        private NetSdrClient _client;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();
            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
        }

        [Test]
        public async Task ConnectAsync_ShouldConnect_WhenNotConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            _tcpMock.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()))
                    .Returns(Task.CompletedTask);

            await _client.ConnectAsync();

            _tcpMock.Verify(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        }

        [Test]
        public async Task ConnectAsync_ShouldNotReconnect_WhenAlreadyConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);

            await _client.ConnectAsync();

            _tcpMock.Verify(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Test]
        public void Disconnect_ShouldCloseTcpConnection()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _client.Disconnect();
            _tcpMock.Verify(t => t.Close(), Times.Once);
        }

        [Test]
        public void Disconnect_ShouldNotThrow_WhenAlreadyDisconnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            Assert.DoesNotThrow(() => _client.Disconnect());
        }

        [Test]
        public async Task StartIQAsync_ShouldBeginReceiving_WhenConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _udpMock.Setup(u => u.StartReceive(It.IsAny<int>(), It.IsAny<Action<byte[]>>()));

            await _client.StartIQAsync();

            _udpMock.Verify(u => u.StartReceive(It.IsAny<int>(), It.IsAny<Action<byte[]>>()), Times.Once);
        }

        [Test]
        public async Task StartIQAsync_ShouldThrow_WhenNotConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            Assert.ThrowsAsync<InvalidOperationException>(() => _client.StartIQAsync());
        }

        [Test]
        public async Task StopIQAsync_ShouldStopReceiving_WhenStarted()
        {
            _udpMock.Setup(u => u.StopReceive());

            await _client.StopIQAsync();

            _udpMock.Verify(u => u.StopReceive(), Times.Once);
        }

        [Test]
        public void Dispose_ShouldCloseConnections_Once()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            _client.Dispose();
            _tcpMock.Verify(t => t.Close(), Times.AtLeastOnce);
        }

        [Test]
        public void Dispose_ShouldBeSafeToCallMultipleTimes()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);

            _client.Dispose();
            Assert.DoesNotThrow(() => _client.Dispose());
        }

        [Test]
        public async Task FullLifecycle_ShouldWorkCorrectly()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            _tcpMock.Setup(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.CompletedTask);
            _udpMock.Setup(u => u.StartReceive(It.IsAny<int>(), It.IsAny<Action<byte[]>>()));
            _udpMock.Setup(u => u.StopReceive());

            await _client.ConnectAsync();
            await _client.StartIQAsync();
            await _client.StopIQAsync();
            _client.Disconnect();

            _tcpMock.Verify(t => t.ConnectAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
            _udpMock.Verify(u => u.StartReceive(It.IsAny<int>(), It.IsAny<Action<byte[]>>()), Times.Once);
            _udpMock.Verify(u => u.StopReceive(), Times.Once);
        }

        [Test]
        public void ShouldThrowArgumentNull_WhenTcpClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NetSdrClient(null, _udpMock.Object));
        }

        [Test]
        public void ShouldThrowArgumentNull_WhenUdpClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NetSdrClient(_tcpMock.Object, null));
        }
    }
}
