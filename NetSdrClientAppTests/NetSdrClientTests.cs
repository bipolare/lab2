using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using Moq;
using NUnit.Framework; 
using System;
using System.Threading.Tasks;

namespace NetSdrClientApp.Tests
{
    // Використовуємо NUnit атрибут [TestFixture]
    [TestFixture] 
    public class NetSdrClientTests
    {
        // Приватні поля
        private Mock<ITcpClient> _tcpClientMock;
        private Mock<IUdpClient> _udpClientMock;
        private NetSdrClient _client;

        // [SetUp] виконується перед кожним тестом для ініціалізації
        [SetUp] 
        public void SetUp()
        {
            _tcpClientMock = new Mock<ITcpClient>();
            _udpClientMock = new Mock<IUdpClient>();

            // Налаштування поведінки за замовчуванням: вважаємо, що клієнт підключений
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client = new NetSdrClient(_tcpClientMock.Object, _udpClientMock.Object);
        }

        // --- Тести для покриття логіки NetSdrClient ---
        
        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenTcpClientIsNull()
        {
            // Перевірка ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(null, _udpClientMock.Object));
        }

        [Test]
        public void Constructor_ThrowsArgumentNullException_WhenUdpClientIsNull()
        {
            // Перевірка ArgumentNullException
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(_tcpClientMock.Object, null));
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
        public async Task SendTcpRequestAsync_ReturnsNull_WhenNotConnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            var testMessage = new byte[] { 0x01 };

            var result = await _client.SendTcpRequestAsync(testMessage);

            Assert.IsNull(result);
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Test]
        public void OnUdpMessageReceived_IgnoresNullData()
        {
            // Імітуємо виклик події з null даними
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, (byte[])null);

            // Перевіряємо, що жоден виняток не був кинутий
            Assert.Pass(); 
        }

        [Test]
        public void OnUdpMessageReceived_IgnoresEmptyData()
        {
            // Імітуємо виклик події з порожнім масивом
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, new byte[0]);

            // Перевіряємо, що жоден виняток не був кинутий
            Assert.Pass();
        }
        
        [Test]
        public void Dispose_DisconnectsAndStopsListening_WhenConnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client.Dispose();

            _tcpClientMock.Verify(c => c.Disconnect(), Times.Once); 
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);
        }
        
        [Test]
        public void Dispose_OnlyStopsListening_WhenDisconnected()
        {
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            _client.Dispose();

            _tcpClientMock.Verify(c => c.Disconnect(), Times.Never); 
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);
        }
    }
}
