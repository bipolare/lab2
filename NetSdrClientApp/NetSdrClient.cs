using NetSdrClientApp.Messages;
using NetSdrClientApp.Networking;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
// 1. Додаємо заборонену залежність для провалу тесту
using EchoServer; 
using static NetSdrClientApp.Messages.NetSdrMessageHelper;

namespace NetSdrClientApp
{
	/// <summary>
	/// Клієнт для взаємодії з NetSDR через TCP/UDP.
	/// </summary>
	public sealed class NetSdrClient : IDisposable
	{
		private readonly ITcpClient _tcpClient;
		private readonly IUdpClient _udpClient;
		private readonly object _lock = new();
		private TaskCompletionSource<byte[]>? _responseTaskSource;

		private const long DefaultSampleRate = 100_000;
		private const ushort AutomaticFilterMode = 0;
		private static readonly byte[] DefaultAdMode = { 0x00, 0x03 };
		private static readonly string SampleFileName = "samples.bin";
        
        // 2. СВІДОМЕ ПОРУШЕННЯ АРХІТЕКТУРИ! 
        // NetSdrClient (Application Layer) не повинен знати про EchoServer (Infrastructure/Test).
        private readonly EchoServer.EchoServer _serverHarness = new EchoServer.EchoServer();


		/// <summary>
		/// Вказує, чи активний прийом IQ-даних.
		/// </summary>
		public bool IQStarted { get; private set; }

		public NetSdrClient(ITcpClient tcpClient, IUdpClient udpClient)
		{
			_tcpClient = tcpClient ?? throw new ArgumentNullException(nameof(tcpClient));
			_udpClient = udpClient ?? throw new ArgumentNullException(nameof(udpClient));

			_tcpClient.MessageReceived += OnTcpMessageReceived;
			_udpClient.MessageReceived += OnUdpMessageReceived;
		}

		/// <summary>
		/// Підключення до SDR-сервера та ініціалізація параметрів.
		/// </summary>
		public async Task ConnectAsync()
		{
			if (_tcpClient.Connected)
				return;

			_tcpClient.Connect();

			var setupMessages = new List<byte[]>
			{
				GetControlItemMessage(
					MsgTypes.SetControlItem, ControlItemCodes.IQOutputDataSampleRate, 
					BitConverter.GetBytes(DefaultSampleRate).Take(5).ToArray()),

				GetControlItemMessage(
					MsgTypes.SetControlItem, ControlItemCodes.RFFilter, 
					BitConverter.GetBytes(AutomaticFilterMode)),

				GetControlItemMessage(
					MsgTypes.SetControlItem, ControlItemCodes.ADModes, DefaultAdMode)
			};

			foreach (var msg in setupMessages)
			{
				await SendTcpRequestAsync(msg).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Відключення від SDR-сервера.
		/// </summary>
		public void Disconnect()
		{
			_tcpClient.Disconnect();
		}

		/// <summary>
		/// Запуск прийому IQ-даних.
		/// </summary>
		public async Task StartIQAsync()
		{
			if (!EnsureConnected())
				return;

			var args = new byte[] { 0x80, 0x02, 0x01, 0x01 };
			var msg = GetControlItemMessage(
				MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, args);

			await SendTcpRequestAsync(msg).ConfigureAwait(false);
			IQStarted = true;

			_ = _udpClient.StartListeningAsync();
		}

		/// <summary>
		/// Зупинка прийому IQ-даних.
		/// </summary>
		public async Task StopIQAsync()
		{
			if (!EnsureConnected())
				return;

			var stopArgs = new byte[] { 0x00, 0x01, 0x00, 0x00 };
			var msg = GetControlItemMessage(
				MsgTypes.SetControlItem, ControlItemCodes.ReceiverState, stopArgs);

			await SendTcpRequestAsync(msg).ConfigureAwait(false);
			IQStarted = false;
			_udpClient.StopListening();
		}

		/// <summary>
		/// Змінює частоту прийому на заданому каналі.
		/// </summary>
		public async Task ChangeFrequencyAsync(long hz, int channel)
		{
			if (!EnsureConnected())
				return;

			var args = new[] { (byte)channel }
				.Concat(BitConverter.GetBytes(hz).Take(5))
				.ToArray();

			var msg = GetControlItemMessage(
				MsgTypes.SetControlItem, ControlItemCodes.ReceiverFrequency, args);

			await SendTcpRequestAsync(msg).ConfigureAwait(false);
		}

		private async void OnUdpMessageReceived(object? sender, byte[] data)
		{
			if (data == null || data.Length == 0)
				return;

			// FIX: Додаємо придушення попередження nullability, оскільки тут очікується, що body буде не null.
			TranslateMessage(
				data, out _, out _, out _, out var body);

			var samples = GetSamples(16, body!); 

			await WriteSamplesAsync(samples).ConfigureAwait(false);
		}

		private static async Task WriteSamplesAsync(IEnumerable<int> samples)
		{
			await using var fs = new FileStream(
				SampleFileName, FileMode.Append, FileAccess.Write, FileShare.Read);

			await using var bw = new BinaryWriter(fs);
			foreach (var sample in samples)
			{
				bw.Write((short)sample);
			}
		}

		private async Task<byte[]?> SendTcpRequestAsync(byte[] msg)
		{
			if (!_tcpClient.Connected)
			{
				Console.WriteLine("No active connection.");
				return null;
			}

			var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

			lock (_lock)
			{
				_responseTaskSource = tcs;
			}

			await _tcpClient.SendMessageAsync(msg).ConfigureAwait(false);

			var response = await tcs.Task.ConfigureAwait(false);
			return response;
		}

		private void OnTcpMessageReceived(object? sender, byte[] e)
		{
			TaskCompletionSource<byte[]>? tcs;
			lock (_lock)
			{
				tcs = _responseTaskSource;
				_responseTaskSource = null;
			}

			tcs?.SetResult(e);
		}

		private bool EnsureConnected()
		{
			if (_tcpClient.Connected)
				return true;

			Console.WriteLine("No active connection.");
			return false;
		}

		public void Dispose()
		{
			_tcpClient.MessageReceived -= OnTcpMessageReceived;
			_udpClient.MessageReceived -= OnUdpMessageReceived;

			if (_tcpClient.Connected)
				_tcpClient.Disconnect();

			_udpClient.StopListening();
		}
	}
}