using System;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipesRcp
{
	public class PipeClient : IDisposable
	{
		private readonly string _serverName;
		private readonly string _pipeName;
		private readonly IPipeMessageSerializer _serializer;
		private NamedPipeClientStream _stream;

		public string ServerName
		{
			get { return _serverName; }
		}

		public string PipeName
		{
			get { return _pipeName; }
		}

		public PipeClient(string serverName, string pipeName, IPipeMessageSerializer serializer)
		{
			if (serverName == null) throw new ArgumentNullException("serverName");
			if (pipeName == null) throw new ArgumentNullException("pipeName");
			if (serializer == null) throw new ArgumentNullException("serializer");

			_serverName = serverName;
			_pipeName = pipeName;
			_serializer = serializer;
		}

		public void Connect()
		{
			_stream = new NamedPipeClientStream(_serverName, _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous, TokenImpersonationLevel.Identification);

			_stream.Connect();
			_stream.ReadMode = PipeTransmissionMode.Message;			
		}

		public async Task<TResponse> SendAsync<TRequest, TResponse>(string messageKey, TRequest request)
		{
			try
			{
				var messageKeyBuffer = Encoding.UTF8.GetBytes(messageKey);
				var sizeBuffer = BitConverter.GetBytes(messageKeyBuffer.Length);
				await _stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length).ConfigureAwait(false);
				await _stream.WriteAsync(messageKeyBuffer, 0, messageKeyBuffer.Length).ConfigureAwait(false);

				var requestBuffer = _serializer.Serialize(request);
				sizeBuffer = BitConverter.GetBytes(requestBuffer.Length);
				await _stream.WriteAsync(sizeBuffer, 0, sizeBuffer.Length).ConfigureAwait(false);
				await _stream.WriteAsync(requestBuffer, 0, requestBuffer.Length).ConfigureAwait(false);

				Console.WriteLine("Write {0} bytes", requestBuffer.Length);

				await _stream.FlushAsync().ConfigureAwait(false);

				var readCount = await _stream.ReadAsync(sizeBuffer, 0, sizeBuffer.Length).ConfigureAwait(false);
				if (readCount == 0)
					return default(TResponse);

				var responseLength = BitConverter.ToInt32(sizeBuffer, 0);
				var responseBuffer = new byte[responseLength];
				readCount = await _stream.ReadAsync(responseBuffer, 0, responseBuffer.Length).ConfigureAwait(false);
				if (readCount == 0)
					return default(TResponse);

				return _serializer.Deserialize<TResponse>(responseBuffer);
			}
			catch (IOException)
			{				
				return default (TResponse);
			}
		}

		public void Dispose()
		{
			if (_stream == null)
				return;

			try
			{
				_stream.WaitForPipeDrain();				
			}
			catch (IOException) { }
			finally
			{
				_stream.Close();
				_stream.Dispose();
			}
		}
	}

	[Serializable]	
	public sealed class EmptyPipeMessage
	{
		private static readonly EmptyPipeMessage _instance = new EmptyPipeMessage();

		public static EmptyPipeMessage Instance
		{
			get { return _instance; }
		}
	}
}