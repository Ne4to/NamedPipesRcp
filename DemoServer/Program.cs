using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NamedPipesRcp;
using NamedPipesRcp.MessagePack;

namespace DemoServer
{
	class Program
	{
		readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
		private PipeServer _pipeServer;

		static void Main(string[] args)
		{
			Program p = new Program();
			p.Start();			
		}

		private void Start()
		{
			Console.CancelKeyPress += Console_CancelKeyPress;
			
			DoWork();

			Console.WriteLine("Press Ctrl-C to exit");
			_stopEvent.WaitOne();

			if (_pipeServer != null)
				_pipeServer.Stop();
		}

		private void DoWork()
		{
			//var serializer = new BinaryFormatterPipeMessageSerializer();
			//var serializer = new MessagePackPipeMessageSerializer();
			var serializer = new DataContractPipeMessageSerializer();
			_pipeServer = new PipeServer("demoPipe", serializer);
			_pipeServer.RegisterHandler<string, string>("TestMessage", OnTestMessage);
			_pipeServer.RegisterRequestOnlyHandler<byte[]>("RequestOnlyBinary", OnRequestOnlyBinary);
			_pipeServer.Start();			
		}

		private string OnTestMessage(string request)
		{
			return "Response " + request;
		}

		private void OnRequestOnlyBinary(byte[] obj)
		{
			Console.WriteLine("Read {0} bytes", obj.Length);
		}

		void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			_stopEvent.Set();
			e.Cancel = true;
		}
	}
}
