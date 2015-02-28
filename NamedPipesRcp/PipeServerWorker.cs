using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NamedPipesRcp
{
	internal class PipeServerWorker : IDisposable
	{
		private readonly string _pipeName;
		private readonly ConcurrentDictionary<string, Func<byte[], byte[]>> _messageFuncs;
		private readonly CancellationToken _cancellationToken;
		private readonly Guid _workerId = Guid.NewGuid();
		private NamedPipeServerStream _stream;

		public string PipeName
		{
			get { return _pipeName; }
		}

		public Guid WorkerId
		{
			get { return _workerId; }
		}

		public event EventHandler ClientDisconnected;

		public PipeServerWorker(string pipeName, ConcurrentDictionary<string, Func<byte[], byte[]>> messageFuncs, CancellationToken cancellationToken)
		{
			if (pipeName == null) throw new ArgumentNullException("pipeName");
			if (messageFuncs == null) throw new ArgumentNullException("messageFuncs");

			_pipeName = pipeName;
			_messageFuncs = messageFuncs;
			_cancellationToken = cancellationToken;
		}

		public async Task<bool> RunAsync()
		{
			try
			{
				_stream = new NamedPipeServerStream(_pipeName,
					PipeDirection.InOut,
					NamedPipeServerStream.MaxAllowedServerInstances,
					PipeTransmissionMode.Message,
					PipeOptions.Asynchronous)
				{
					ReadMode = PipeTransmissionMode.Message,
				};

				await _stream.WaitForConnectionAsync().ConfigureAwait(false);

				ProcessMessages();

				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private async void ProcessMessages()
		{
			while (!_cancellationToken.IsCancellationRequested)
			{
				try
				{
					var sizeBuffer = new byte[4];
					var readCount = await _stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length, _cancellationToken).ConfigureAwait(false);
					if (readCount == 0)
					{
						RaiseDisconnected();
						break;
					}

					var messageKeyLength = BitConverter.ToInt32(sizeBuffer, 0);
					var messageKeyBuffer = new byte[messageKeyLength];
					readCount = await _stream.ReadAsync(messageKeyBuffer, 0, messageKeyBuffer.Length, _cancellationToken).ConfigureAwait(false);
					if (readCount == 0)
					{
						RaiseDisconnected();
						break;
					}

					var messageKey = Encoding.UTF8.GetString(messageKeyBuffer, 0, readCount);

					readCount = await _stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length, _cancellationToken).ConfigureAwait(false);
					if (readCount == 0)
					{
						RaiseDisconnected();
						break;
					}

					var dataLength = BitConverter.ToInt32(sizeBuffer, 0);
					var buffer = new byte[dataLength];
					readCount = await _stream.ReadAsync(buffer, 0, buffer.Length, _cancellationToken).ConfigureAwait(false);
					if (readCount == 0)
					{
						RaiseDisconnected();
						break;
					}

					Func<byte[], byte[]> func;
					if (_messageFuncs.TryGetValue(messageKey, out func))
					{
						var responseBuffer = func(buffer);

						sizeBuffer = BitConverter.GetBytes(responseBuffer.Length);
						await _stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length, _cancellationToken).ConfigureAwait(false);

						await _stream.WriteAsync(responseBuffer, 0, responseBuffer.Length, _cancellationToken).ConfigureAwait(false);
						await _stream.FlushAsync(_cancellationToken).ConfigureAwait(false);
					}
				}
				catch (TaskCanceledException)
				{
					RaiseDisconnected();
					break;
				}
				catch (IOException)
				{
					RaiseDisconnected();
					break;
				}
			}
		}

		private void RaiseDisconnected()
		{
			var handler = ClientDisconnected;
			if (handler != null)
				handler(this, EventArgs.Empty);
		}

		public void Dispose()
		{
			if (_stream == null)
				return;

			try
			{
				_stream.WaitForPipeDrain();
				_stream.Disconnect();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (IOException)
			{
			}
			finally
			{
				_stream.Close();
				_stream.Dispose();
			}
		}
	}
}