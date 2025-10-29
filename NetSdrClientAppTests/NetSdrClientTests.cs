using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace NetSdrClientAppTests
{
    [TestFixture]
    public class NetSdrClientTests
    {
        private NetSdrClient _client;
        private Mock<ITcpClient> _tcpMock;
        private Mock<IUdpClient> _udpMock;

        [SetUp]
        public void Setup()
        {
            _tcpMock = new Mock<ITcpClient>();
            _udpMock = new Mock<IUdpClient>();

            _tcpMock.Setup(tcp => tcp.Connect()).Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(true);
            });

            _tcpMock.Setup(tcp => tcp.Disconnect()).Callback(() =>
            {
                _tcpMock.Setup(t => t.Connected).Returns(false);
            });

            _tcpMock.Setup(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()))
                .Callback<byte[]>((bytes) =>
                {
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
                })
                .Returns(Task.CompletedTask);

            _udpMock.Setup(u => u.StartListeningAsync()).Returns(Task.CompletedTask);
            _udpMock.Setup(u => u.StopListening());

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        // ------------------ Основные тесты ------------------

        [Test]
        public async Task ConnectAsync_ShouldConnectAndSendThreeMessages()
        {
            await _client.ConnectAsync();
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
            _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
            Assert.That(_client.IsConnected, Is.True);
        }

        [Test]
        public async Task ConnectAsync_ShouldHandleAlreadyConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            await _client.ConnectAsync();
            _tcpMock.Verify(tcp => tcp.Connect(), Times.Never);
        }

        [Test]
        public void Disconnect_ShouldWork_WhenNotConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            _client.Disconnect();
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
        }

        [Test]
        public async Task Disconnect_ShouldWork_WhenConnected()
        {
            await _client.ConnectAsync();
            _client.Disconnect();
            _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
            Assert.That(_client.IsConnected, Is.False);
        }

        [Test]
        public async Task StartIQAsync_ShouldNotStart_WhenNotConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            await _client.StartIQAsync();
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Never);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task StartIQAsync_ShouldStart_WhenConnected()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            _udpMock.Verify(u => u.StartListeningAsync(), Times.Once);
            Assert.That(_client.IQStarted, Is.True);
        }

        [Test]
        public async Task StopIQAsync_ShouldStop_WhenStarted()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            await _client.StopIQAsync();
            _udpMock.Verify(u => u.StopListening(), Times.Once);
            Assert.That(_client.IQStarted, Is.False);
        }

        [Test]
        public async Task StopIQAsync_ShouldHandle_WhenNotStarted()
        {
            await _client.ConnectAsync();
            await _client.StopIQAsync();
            _udpMock.Verify(u => u.StopListening(), Times.Once);
        }

        [Test]
        public void Dispose_ShouldCallDisconnect()
        {
            _client.Dispose();
            _tcpMock.Verify(t => t.Disconnect(), Times.AtLeastOnce);
        }

        [Test]
        public async Task ShouldHandle_MessageReceivedEvent()
        {
            await _client.ConnectAsync();
            var message = new byte[] { 0x01, 0x02 };
            _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, message);
            Assert.Pass("MessageReceived event handled without exception");
        }

        // ------------------ Дополнительные тесты для 80%+ покрытия ------------------

        [Test]
        public void Constructor_ShouldThrow_WhenTcpClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NetSdrClient(null, _udpMock.Object));
        }

        [Test]
        public void Constructor_ShouldThrow_WhenUdpClientIsNull()
        {
            Assert.Throws<ArgumentNullException>(() => new NetSdrClient(_tcpMock.Object, null));
        }

        [Test]
        public async Task StartIQAsync_ShouldNotThrow_WhenAlreadyStarted()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            Assert.DoesNotThrowAsync(() => _client.StartIQAsync());
        }

        [Test]
        public async Task StopIQAsync_ShouldBeSafe_WhenCalledTwice()
        {
            await _client.ConnectAsync();
            await _client.StartIQAsync();
            await _client.StopIQAsync();
            Assert.DoesNotThrowAsync(() => _client.StopIQAsync());
        }

        [Test]
        public void Dispose_ShouldBeSafe_WhenCalledMultipleTimes()
        {
            _client.Dispose();
            Assert.DoesNotThrow(() => _client.Dispose());
        }

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }
    }
}
