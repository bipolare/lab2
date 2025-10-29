using NetSdrClientApp;
using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using Moq;
using Xunit;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace NetSdrClientApp.Tests
{
    // Цей клас потребує реальних/мок-даних для NetSdrMessageHelper, 
    // але для покриття логіки достатньо моків TCP/UDP
    public class NetSdrClientTests
    {
        // Приватні поля для мок-об'єктів
        private readonly Mock<ITcpClient> _tcpClientMock;
        private readonly Mock<IUdpClient> _udpClientMock;
        private readonly NetSdrClient _client;

        public NetSdrClientTests()
        {
            // Налаштування моків перед кожним тестом
            _tcpClientMock = new Mock<ITcpClient>();
            _udpClientMock = new Mock<IUdpClient>();

            // Налаштування поведінки за замовчуванням: вважаємо, що клієнт підключений, 
            // якщо тест не вказує інше.
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client = new NetSdrClient(_tcpClientMock.Object, _udpClientMock.Object);
        }

        // --- Покриття рядків 36, 37 (Перевірка ArgumentNullException) ---
        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenTcpClientIsNull()
        {
            // Рядок 36: _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(null, _udpClientMock.Object));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullException_WhenUdpClientIsNull()
        {
            // Рядок 37: _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
            Assert.Throws<ArgumentNullException>(() => 
                new NetSdrClient(_tcpClientMock.Object, null));
        }

        // --- Покриття рядків 104, 121, 131, 149 (Перевірка стану "Не підключено") ---
        [Fact]
        public async Task StartIQAsync_DoesNothing_WhenNotConnected()
        {
            // Налаштовуємо мок, щоб імітувати відсутність з'єднання
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            // Рядок 104: if (!EnsureConnected()) return;
            await _client.StartIQAsync();

            // Перевіряємо, що ми не намагалися відправити повідомлення
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task StopIQAsync_DoesNothing_WhenNotConnected()
        {
            // Рядок 121: if (!EnsureConnected()) return;
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            await _client.StopIQAsync();

            // Перевіряємо, що ми не намагалися відправити повідомлення
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        [Fact]
        public async Task ChangeFrequencyAsync_DoesNothing_WhenNotConnected()
        {
            // Рядок 131: if (!EnsureConnected()) return;
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            await _client.ChangeFrequencyAsync(100000, 1);

            // Перевіряємо, що ми не намагалися відправити повідомлення
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }
        
        [Fact]
        public async Task SendTcpRequestAsync_ReturnsNull_WhenNotConnected()
        {
            // Рядок 149: if (!_tcpClient.Connected)
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            var testMessage = new byte[] { 0x01 };

            var result = await _client.SendTcpRequestAsync(testMessage);

            // Перевіряємо, що повертається null
            Assert.Null(result);
            // Перевіряємо, що не було спроби відправки
            _tcpClientMock.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        }

        // --- Покриття рядків 136, 137 (Перевірка некоректних UDP даних) ---
        [Fact]
        public void OnUdpMessageReceived_IgnoresNullData()
        {
            // Рядок 136: if (data == null || data.Length == 0) return;
            // Імітуємо виклик події з null даними
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, (byte[])null);

            // Тут ми б перевірили, що жодні внутрішні статичні методи (TranslateMessage, WriteSamplesAsync) не викликалися,
            // але оскільки вони статичні, це складно. Ми просто перевіряємо, що виклик не кидає винятку.
            // Щоб покрити ці рядки, достатньо виклику.
        }

        [Fact]
        public void OnUdpMessageReceived_IgnoresEmptyData()
        {
            // Рядок 136: if (data == null || data.Length == 0) return;
            // Імітуємо виклик події з порожнім масивом
            _udpClientMock.Raise(c => c.MessageReceived += null, _udpClientMock.Object, new byte[0]);

            // Аналогічно, просто перевіряємо, що виклик не кидає винятку.
        }
        
        // --- Покриття рядків 161, 163, 164 (Dispose під час з'єднання) ---
        [Fact]
        public void Dispose_DisconnectsAndStopsListening_WhenConnected()
        {
            // Налаштовуємо, що клієнт підключений
            _tcpClientMock.SetupGet(c => c.Connected).Returns(true);
            
            _client.Dispose();

            // Рядок 163: _tcpClient.Disconnect();
            _tcpClientMock.Verify(c => c.Disconnect(), Times.Once); 
            // Рядок 164: _udpClient.StopListening();
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);
        }
        
        [Fact]
        public void Dispose_OnlyStopsListening_WhenDisconnected()
        {
            // Налаштовуємо, що клієнт відключений
            _tcpClientMock.SetupGet(c => c.Connected).Returns(false);
            
            _client.Dispose();

            // Рядок 161: if (_tcpClient.Connected) - не спрацює
            _tcpClientMock.Verify(c => c.Disconnect(), Times.Never); 
            // Рядок 164: _udpClient.StopListening();
            _udpClientMock.Verify(c => c.StopListening(), Times.Once);
        }
    }
}
