using Moq;
using NetSdrClientApp;
using System.Threading.Tasks;
using Xunit;
using System;
using System.Linq;

// --- ИНТЕРФЕЙСЫ (Поместите это в отдельный файл или оставьте здесь, если это тестовый проект) ---
// Эти интерфейсы нужны для "подмены" реальной сетевой логики в тестах.
namespace NetSdrClientApp.Networking
{
    public interface ITcpClient
    {
        bool Connected { get; }
        void Connect();
        void Disconnect();
        Task SendMessageAsync(byte[] message);
        event EventHandler<byte[]>? MessageReceived;
    }

    public interface IUdpClient
    {
        Task StartListeningAsync();
        void StopListening();
        event EventHandler<byte[]>? MessageReceived;
    }
}
// ---------------------------------------------------------------------------------------------------------------


namespace NetSdrClientAppTests
{
    // Тесты, которые гарантируют покрытие всех веток "No active connection."
    public class NetSdrClientTests
    {
        // Тест 1: Покрытие StartIQAsync() и SendTcpRequest() при отсутствии соединения.
        [Fact]
        public async Task StartIQAsync_NoConnection_CoversConsoleWriteLineAndReturns()
        {
            // --- Setup ---
            var mockTcpClient = new Mock<ITcpClient>();
            // Установка .Connected в false при мокировании ITcpClient.
            mockTcpClient.SetupGet(c => c.Connected).Returns(false); 
            var mockUdpClient = new Mock<IUdpClient>();

            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);
            
            // --- Act ---
            // Вызов метода, который должен сразу выйти
            await client.StartIQAsync();

            // --- Assert ---
            // Проверка, что код в SendTcpRequest() и StartIQAsync() не был выполнен
            mockTcpClient.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
            mockUdpClient.Verify(u => u.StartListeningAsync(), Times.Never());
            Assert.False(client.IQStarted); 
        }
        
        // Тест 2: Покрытие StopIQAsync() при отсутствии соединения.
        [Fact]
        public async Task StopIQAsync_NoConnection_CoversConsoleWriteLineAndReturns()
        {
            // --- Setup ---
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.SetupGet(c => c.Connected).Returns(false); 
            var mockUdpClient = new Mock<IUdpClient>();

            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);
            client.IQStarted = true; // Установка состояния для проверки, что оно не изменится

            // --- Act ---
            await client.StopIQAsync();

            // --- Assert ---
            // Проверка, что StopIQAsync() вышел до вызова SendTcpRequest
            mockTcpClient.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
            mockUdpClient.Verify(u => u.StopListening(), Times.Never());
            Assert.True(client.IQStarted); // Проверка, что флаг не сбросился
        }

        // Тест 3: Покрытие ChangeFrequencyAsync() при отсутствии соединения.
        [Fact]
        public async Task ChangeFrequencyAsync_NoConnection_CoversConsoleWriteLineAndReturns()
        {
            // --- Setup ---
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.SetupGet(c => c.Connected).Returns(false); 
            var mockUdpClient = new Mock<IUdpClient>();

            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);

            // --- Act ---
            await client.ChangeFrequencyAsync(1000000, 1);

            // --- Assert ---
            // Проверка, что SendTcpRequest не был вызван
            mockTcpClient.Verify(c => c.SendMessageAsync(It.IsAny<byte[]>()), Times.Never());
        }

        // Тест 4: Покрытие SendTcpRequest() при отсутствии соединения.
        [Fact]
        public async Task SendTcpRequest_NoConnection_ReturnsNullAndCoversConsoleWriteLine()
        {
            // --- Setup ---
            var mockTcpClient = new Mock<ITcpClient>();
            mockTcpClient.SetupGet(c => c.Connected).Returns(false); 
            var mockUdpClient = new Mock<IUdpClient>();

            var client = new NetSdrClient(mockTcpClient.Object, mockUdpClient.Object);

            byte[] dummyRequest = { 0x01 };

            // --- Act ---
            // Вызов приватного метода через рефлексию (стандартный прием для тестирования приватных методов).
            var actualResponse = await client.GetType()
                                           .GetMethod("SendTcpRequest", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                                           .Invoke(client, new object[] { dummyRequest }) as Task<byte[]>;

            // --- Assert ---
            // Проверка, что метод вернул null
            Assert.Null(await actualResponse);
            // Проверка, что сообщение не было отправлено
            mockTcpClient.Verify(c => c.SendMessageAsync(dummyRequest), Times.Never());
        }
    }
}
