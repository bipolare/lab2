using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using System.Threading.Tasks;
using Xunit;

// Убедитесь, что в вашем тестовом проекте установлены пакеты Moq и xUnit.

namespace NetSdrClientAppTests
{
    public class NetSdrClientTests
    {
        // Задача: Покрыть ветку 'if (!_tcpClient.Connected)' в StartIQAsync() и SendTcpRequest().
        
        [Fact]
        public async Task StartIQAsync_NoConnection_HandlesGracefully()
        {
            // --- Setup ---
            
            // 1. Мокируем ITcpClient и настраиваем, чтобы Connected всегда возвращал false
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.SetupGet(c => c.Connected).Returns(false);
            
            // 2. Мокируем IUdpClient (не используется в этом сценарии, но нужен для конструктора)
            var mockUdpClient = new Mock<IUdpClient>();

            // 3. Создаем экземпляр клиента
            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);

            // --- Act ---
            // Вызываем метод, который содержит непокрытую ветку
            await client.StartIQAsync();

            // --- Assert ---
            
            // Проверяем, что SendMessageAsync НИКОГДА не был вызван,
            // поскольку код должен был выйти по 'return;'
            mockTcpClient.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
            
            // Проверяем, что StartListeningAsync НИКОГДА не был вызван
            mockUdpClient.Verify(u => u.StartListeningAsync(), Times.Never());

            // Убедитесь, что IQStarted не установился в true
            Assert.False(client.IQStarted);

            // Если тест пройдет успешно, это покроет ветку в StartIQAsync().
            // Поскольку SendTcpRequest вызывается из StartIQAsync, и он тоже делает проверку,
            // этот один тест, вероятно, покроет обе непокрытые строки (StartIQAsync и SendTcpRequest).
        }
        
        [Fact]
        public async Task StopIQAsync_NoConnection_HandlesGracefully()
        {
            // --- Setup ---
            
            // 1. Мокируем ITcpClient и настраиваем, чтобы Connected всегда возвращал false
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.SetupGet(c => c.Connected).Returns(false);
            
            // 2. Мокируем IUdpClient (не используется в этом сценарии, но нужен для конструктора)
            var mockUdpClient = new Mock<IUdpClient>();

            // 3. Создаем экземпляр клиента
            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);

            // --- Act ---
            // Вызываем метод, который содержит непокрытую ветку
            await client.StopIQAsync();

            // --- Assert ---
            
            // Проверяем, что SendMessageAsync НИКОГДА не был вызван,
            // поскольку код должен был выйти по 'return;'
            mockTcpClient.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
            
            // Проверяем, что StopListening НИКОГДА не был вызван
            mockUdpClient.Verify(u => u.StopListening(), Times.Never());
        }
    }
}
