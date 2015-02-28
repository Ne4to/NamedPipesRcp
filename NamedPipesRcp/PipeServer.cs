using System;
using System.Collections.Concurrent;
using System.IO.Pipes;
using System.Threading;

namespace NamedPipesRcp
{
	public class PipeServer
	{
		#region Fields

		private readonly string _pipeName;
		private readonly IPipeMessageSerializer _serializer;
		private readonly PipeSecurity _pipeSecurity;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		private readonly ConcurrentDictionary<string, Func<byte[], byte[]>> _messageFuncs = new ConcurrentDictionary<string, Func<byte[], byte[]>>();
		private readonly ConcurrentDictionary<Guid, PipeServerWorker> _workers = new ConcurrentDictionary<Guid, PipeServerWorker>();

		#endregion
		
		#region Properties

		public string PipeName
		{
			get { return _pipeName; }
		}

		#endregion
		
		public PipeServer(string pipeName, IPipeMessageSerializer serializer, PipeSecurity pipeSecurity = null)
		{
			if (pipeName == null) throw new ArgumentNullException("pipeName");
			if (serializer == null) throw new ArgumentNullException("serializer");
			
			_pipeName = pipeName;
			_serializer = serializer;
			_pipeSecurity = pipeSecurity;
		}

		public async void Start()
		{
			while (!_cancellationTokenSource.Token.IsCancellationRequested)
			{
				var worker = new PipeServerWorker(_pipeName, _pipeSecurity, _messageFuncs, _cancellationTokenSource.Token);

				if (await worker.RunAsync())
				{
					worker.ClientDisconnected += worker_ClientDisconnected;
					_workers.TryAdd(worker.WorkerId, worker);					
				}
				else
				{
					worker.Dispose();
					break;
				}
			}
		}

		public bool RegisterHandler<TRequest, TResponse>(string messageKey, Func<TRequest, TResponse> func)
		{
			if (messageKey == null) throw new ArgumentNullException("messageKey");
			if (func == null) throw new ArgumentNullException("func");

			var bytesFunc = new Func<byte[], byte[]>(requestBuffer => FuncWrapper(func, requestBuffer));
			return _messageFuncs.TryAdd(messageKey, bytesFunc);
		}

		public bool RegisterRequestOnlyHandler<TRequest>(string messageKey, Action<TRequest> func)
		{
			Func<TRequest, EmptyPipeMessage> bytesFunc = request =>
			{
				func(request);
				return EmptyPipeMessage.Instance;
			};

			return RegisterHandler(messageKey, bytesFunc);
		}

		public bool RegisterResponseOnlyHandler<TResponse>(string messageKey, Func<TResponse> func)
		{
			Func<EmptyPipeMessage, TResponse> bytesFunc = request => func();

			return RegisterHandler(messageKey, bytesFunc);
		}

		byte[] FuncWrapper<TRequest, TResponse>(Func<TRequest, TResponse> func, byte[] requestBuffer)
		{
			var request = _serializer.Deserialize<TRequest>(requestBuffer);
			var response = func(request);
			return _serializer.Serialize(response);
		}

		void worker_ClientDisconnected(object sender, EventArgs e)
		{
			var worker = (PipeServerWorker)sender;

			worker.ClientDisconnected -= worker_ClientDisconnected;
			worker.Dispose();

			PipeServerWorker temp;
			_workers.TryRemove(worker.WorkerId, out temp);			
		}

		public void Stop()
		{
			_cancellationTokenSource.Cancel();

			foreach (var worker in _workers.Values)
			{
				worker.ClientDisconnected -= worker_ClientDisconnected;
				worker.Dispose();
			}

			_workers.Clear();
		}
	}
}
