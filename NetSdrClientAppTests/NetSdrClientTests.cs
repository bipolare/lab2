using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using Moq;
using NUnit.Framework; 
using System;
using System.Threading.Tasks;
using System.Linq; // Додано для використання .ToArray()

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
        public async Task ConnectAsync_SendsConfigurationMessages()
        {
            // Arrange
            // Переконаємося, що на початку він не підключений, щоб спрацювало Connect()
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false).;

            // Act
            await _client.ConnectAsync();

            // Assert
            // 1. Перевіряємо, чи був викликаний метод підключення
            _tcpClientMock.Verify(c => c.Connect(), Times.Once);
            
            // 2. Перевіряємо, чи були відправлені 3 конфігураційні повідомлення
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
        }

        [Test]
        public async Task StartIQAsync_SendsCorrectMessageAndStartsListening()
        {
            // Act
            await _client.StartIQAsync();

            // Assert
            // Перевіряємо, чи було відправлено повідомлення
            _tcpClientMock.Verify(
                c => c.SendMessageAsync(It.Is<byte[]>(msg => 
                    msg.Skip(10).Take(4).SequenceEqual(new byte[] { 0x80, 0x02, 0x01, 0x01 }))), // Перевіряємо частину з args
                Times.Once,
                "Start IQ message was not sent correctly."
            );
            
            // Перевіряємо, чи було запущено прослуховування UDP
            _udpClientMock.Verify(c => c.StartListeningAsync(), Times.Once);
            
            // Перевіряємо прапорець стану
            Assert.That(_client.IQStarted, Is.True);
        }

        [Test]
        public async Task StopIQAsync_SendsCorrectMessageAndStopsListening()
        {
            // Act
            await _client.StopIQAsync();

            // Assert
            // Перевіряємо, чи було відправлено повідомлення
            _tcpClientMock.Verify(
                c => c.SendMessageAsync(It.Is<byte[]>(msg => 
                    msg.Skip(10).Take(4).SequenceEqual(new byte[] { 0x00, 0x01, 0x00, 0x00 }))), // Перевіряємо частину з args
                Times.Once,
                "Stop IQ message was not sent correctly."
            );
            
            // Перевіряємо, чи було зупинено прослуховування UDP
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);

            // Перевіряємо прапорець стану
            Assert.That(_client.IQStarted, Is.False);
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
