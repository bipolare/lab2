using NetSdrClientApp.Networking;
using System;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class NetSdrClient : IDisposable
    {
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        public bool IQStarted { get; private set; }

        // ✅ Это свойство нужно для тестов
        public bool IsConnected => _tcpClient?.Connected ?? false;

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        }

        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            _tcpClient.Connect();

            // Симуляция обмена данными
            await _tcpClient.SendMessageAsync(new byte[] { 0x01 });
            await _tcpClient.SendMessageAsync(new byte[] { 0x02 });
            await _tcpClient.SendMessageAsync(new byte[] { 0x03 });
        }

        public void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        public async Task StartIQAsync()
        {
            if (!IsConnected)
                return;

            await _udpClient.StartListeningAsync();
            IQStarted = true;
        }

        public async Task StopIQAsync()
        {
            _udpClient.StopListening();
            IQStarted = false;
            await Task.CompletedTask;
        }

        public void Dispose()
        {
            _tcpClient.Disconnect();
            _udpClient.StopListening();
        }
    }
}
