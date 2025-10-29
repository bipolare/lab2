using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using Moq;
using NUnit.Framework; 
using System;
using System.Threading.Tasks;

namespace NetSdrClientApp.Tests
{
    [TestFixture] 
    public class NetSdrClientTests
    {
        private Mock<ITcpClient> _tcpClientMock;
        private Mock<IUdpClient> _udpClientMock;
        private NetSdrClient _client = null!; // Придушуємо попередження, оскільки ініціалізація відбувається в SetUp

        [SetUp] 
        public void SetUp()
        {
            _tcpClientMock = new Mock<ITcpClient>();
            _udpClientMock = new Mock<IUdpClient>();

            // Налаштування поведінки за замовчуванням
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client = new NetSdrClient(_tcpClientMock.Object, _udpClientMock.Object);
        }
        
        // FIX for NUnit1032: Додаємо [TearDown] для коректної утилізації _client
        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
        }

        // --- Тести для покриття логіки NetSdrClient ---
        
        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenTcpClientIsNull()
        {
            // Використовуємо null! для придушення попередження
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(null!, _udpClientMock.Object));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenUdpClientIsNull()
        {
            // Використовуємо null!
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(_tcpClientMock.Object, null!));
        }

        [Test]
        public async Task StartIQAsync_DoesNothing_WhenNotConnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            await _client.StartIQAsync();

            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task StopIQAsync_DoesNothing_WhenNotConnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            await _client.StopIQAsync();

            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public async Task ChangeFrequencyAsync_DoesNothing_WhenNotConnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            await _client.ChangeFrequencyAsync(100000, 1);

            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }
        
        [Test]
        public void OnUdpMessageReceived_IgnoresNullData()
        {
            // Імітуємо виклик події з null даними.
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, (byte[]?)null);

            Assert.Pass(); 
        }

        [Test]
        public void OnUdpMessageReceived_IgnoresEmptyData()
        {
            // Імітуємо виклик події з порожнім масивом
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, new byte[0]);

            Assert.Pass();
        }
        
        [Test]
        public void Dispose_DisconnectsAndStopsListening()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client.Dispose();

            _tcpClientMock.Verify(c => c.Disconnect(), Times.Once); 
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);
        }
    }
}
