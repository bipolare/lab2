using Moq;
using NetSdrClientApp;
using NetSdrClientApp.Networking;
using NUnit.Framework;
using System.Threading.Tasks;

namespace NetSdrClientAppTests;

public class NetSdrClientTests
{
    private NetSdrClient _client;
    private Mock<ITcpClient> _tcpMock;
    private Mock<IUdpClient> _udpMock;

    [SetUp]
    public void Setup()
    {
        // TCP мок
        _tcpMock = new Mock<ITcpClient>();
        _tcpMock.Setup(tcp => tcp.Connected).Returns(false);
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
                    // Імітуємо подію отримання повідомлення
                    _tcpMock.Raise(tcp => tcp.MessageReceived += null, _tcpMock.Object, bytes);
                });

        // UDP мок
        _udpMock = new Mock<IUdpClient>();

        // Створюємо клієнт
        _client = new NetSdrClient(_tcpMock.Object, _udpMock.Object);
    }

    // Приватний метод для підключення клієнта
    private async Task ConnectClientAsync()
    {
        await _client.ConnectAsync();
    }

    [Test]
    public async Task ConnectAsyncTest()
    {
        // act
        await ConnectClientAsync();

        // assert
        _tcpMock.Verify(tcp => tcp.Connect(), Times.Once);
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Exactly(3));
    }

    [Test]
    public void DisconnectWithNoConnectionTest()
    {
        // act
        _client.Disconnect();

        // assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task DisconnectTest()
    {
        // arrange
        await ConnectClientAsync();

        // act
        _client.Disconnect();

        // assert
        _tcpMock.Verify(tcp => tcp.Disconnect(), Times.Once);
    }

    [Test]
    public async Task StartIQNoConnectionTest()
    {
        // act
        await _client.StartIQAsync();

        // assert
        _tcpMock.Verify(tcp => tcp.SendMessageAsync(It.IsAny<byte[]>()), Times.Never);
        _tcpMock.VerifyGet(tcp => tcp.Connected, Times.AtLeastOnce);
    }

    [Test]
    public async Task StartIQTest()
    {
        // arrange
        await ConnectClientAsync();

        // act
        await _client.StartIQAsync();

        // assert
        _udpMock.Verify(udp => udp.StartListeningAsync(), Times.Once);
        Assert.That(_client.IQStarted, Is.True);
    }

    [Test]
    public async Task StopIQTest()
    {
        // arrange
        await ConnectClientAsync();
        await _client.StartIQAsync();

        // act
        await _client.StopIQAsync();

        // assert
        _udpMock.Verify(udp => udp.StopListening(), Times.Once);
        Assert.That(_client.IQStarted, Is.False);
    }

    // --------------------------
    // TearDown для очищення ресурсів
    [TearDown]
    public void Cleanup()
    {
        _client?.Dispose();
    }
}
