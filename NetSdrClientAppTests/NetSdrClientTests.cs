using NetSdrClientApp.Networking;
using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Text;
using EchoServer; // !!! ЦЕЙ using СТВОРЮЄ ПОРУШЕННЯ АРХІТЕКТУРИ (ЧЕРВОНИЙ PR) !!!

namespace NetSdrClientApp
{
    public class NetSdrClient : IDisposable
    {
        private readonly ITcpClient _tcpClient;
        private readonly IUdpClient _udpClient;

        public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
        {
            _tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
            _udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));

            // !!! ЦЕЙ РЯДОК СТВОРЮЄ ПОРУШЕННЯ АРХІТЕКТУРИ (ЧЕРВОНИЙ PR) !!!
            // Ми використовуємо клас EchoServer, змушуючи NetSdrClientApp залежати від EchoServer.
            var securityBreach = new EchoServer(); 
        }

        public async Task ConnectAsync(string ipAddress, int tcpPort, int udpPort)
        {
            await _tcpClient.ConnectAsync(ipAddress, tcpPort);
            _udpClient.StartListening(udpPort);
        }

        public void Disconnect()
        {
            _tcpClient.Disconnect();
            _udpClient.StopListening();
        }

        public async Task StartIQAsync()
        {
            if (_tcpClient.Connected)
            {
                // Приклад: відправка команди Start IQ
                byte[] message = Encoding.ASCII.GetBytes("start_iq\n");
                await _tcpClient.SendMessageAsync(message);
            }
        }

        public async Task StopIQAsync()
        {
            if (_tcpClient.Connected)
            {
                // Приклад: відправка команди Stop IQ
                byte[] message = Encoding.ASCII.GetBytes("stop_iq\n");
                await _tcpClient.SendMessageAsync(message);
            }
        }

        public async Task ChangeFrequencyAsync(long frequency, int channel)
        {
            if (_tcpClient.Connected)
            {
                // Приклад: відправка команди зміни частоти
                string command = $"set_freq {channel} {frequency}\n";
                byte[] message = Encoding.ASCII.GetBytes(command);
                await _tcpClient.SendMessageAsync(message);
            }
        }

        private void OnUdpMessageReceived(object? sender, byte[]? data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }
            
            // Обробка UDP даних
            Console.WriteLine($"UDP Data Received: {BitConverter.ToString(data)}");
        }

        public void Dispose()
        {
            Disconnect();
            GC.SuppressFinalize(this);
        }
    }
}