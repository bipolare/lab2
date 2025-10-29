using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
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

            _tcpMock.Setup(t => t.Connect()).Callback(() => _tcpMock.Setup(t => t.Connected).Returns(true));
            _tcpMock.Setup(t => t.Disconnect()).Callback(() => _tcpMock.Setup(t => t.Connected).Returns(false));
            _tcpMock.Setup(t => t.SendMessageAsync(It.IsAny<byte[]>()))
                    .Callback<byte[]>(bytes => _tcpMock.Raise(t => t.MessageReceived += null, _tcpMock.Object, bytes))
                    .Returns(Task.CompletedTask);

            _udpMock.Setup(u => u.StartListeningAsync()).Returns(Task.CompletedTask);
            _udpMock.Setup(u => u.StopListening()).Returns(Task.CompletedTask);

            _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
        }

        [Test]
        public async Task ConnectAsync_ShouldConnectAndSendThreeMessages()
        {
            await _client.ConnectAsync();

            _tcpMock.Verify(t => t.Connect(), Times.Once);
            _tcpMock.Verify(t => t.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
            Assert.That(_client.IsConnected, Is.True);
        }

        [Test]
        public async Task ConnectAsync_ShouldHandleAlreadyConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(true);
            await _client.ConnectAsync();
            _tcpMock.Verify(t => t.Connect(), Times.Never);
        }

        [Test]
        public void Disconnect_ShouldWork_WhenNotConnected()
        {
            _tcpMock.Setup(t => t.Connected).Returns(false);
            _client.Disconnect();
            _tcpMock.Verify(t => t.Disconnect(), Times.Once);
        }

        [Test]
        public async Task Disconnect_ShouldWork_WhenConnected()
        {
            await _client.ConnectAsync();
            _client.Disconnect();
            _tcpMock.Verify(t => t.Disconnect(), Times.Once);
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

        [TearDown]
        public void Cleanup()
        {
            _client.Dispose();
        }
    }
}
