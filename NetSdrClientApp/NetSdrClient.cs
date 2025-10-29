using NetSdrClientApp.Networking;
using System;
using System.Threading.Tasks;

namespace NetSdrClientApp
{
    public class NetSdrClient : IDisposable
    {
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        public bool IsConnected => _tcpClient.Connected;
        public bool IQStarted { get; private set; }

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));
        }

        public async Task ConnectAsync()
        {
            if (!_tcpClient.Connected)
            {
                _tcpClient.Connect();
                // Отправляем три сообщения после подключения
                for (int i = 0; i < 3; i++)
                {
                    await _tcpClient.SendMessageAsync(new byte[] { 0x00 });
                }
            }
        }

        public void Disconnect()
        {
            _tcpClient.Disconnect();
        }

        public async Task StartIQAsync()
        {
            if (!IsConnected) return;

            await _udpClient.StartListeningAsync();
            IQStarted = true;
        }

        public async Task StopIQAsync()
        {
            await _udpClient.StopListening();
            IQStarted = false;
        }

        public void Dispose()
        {
            Disconnect();
        }

        // Метод для обработки событий от UDP
        public void OnUdpMessageReceived(byte[] message)
        {
            // Здесь можно обработать сообщение
        }
    }
}
