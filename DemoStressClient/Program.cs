using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using NamedPipesRcp;
using NamedPipesRcp.MessagePack;

namespace DemoStressClient
{
	class Program
	{
		private const int ClientCount = 20;
		readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);
		private static bool _stopRequested;

		static void Main(string[] args)
		{
			Program p = new Program();
			p.Start();
		}

		private void Start()
		{
			Console.CancelKeyPress += Console_CancelKeyPress;
			Console.WriteLine("Using {0} clients", ClientCount);
			Console.WriteLine("Press Ctrl-C to exit");

			List<Thread> threads = new List<Thread>(ClientCount);

			foreach (var i in Enumerable.Range(1, ClientCount))
			{
				var thread = new Thread(DoWork);
				threads.Add(thread);

				thread.Start(i);
			}

			Console.WriteLine("All threads started");

			_stopEvent.WaitOne();

			foreach (var thread in threads)
			{
				thread.Join();
			}
		}

		private async void DoWork(object o)
		{
			Thread.Sleep((int)o);

			//var serializer = new BinaryFormatterPipeMessageSerializer();
			//var serializer = new MessagePackPipeMessageSerializer();
			var serializer = new DataContractPipeMessageSerializer();
			var s = new PipeClient(".", "demoPipe", serializer);

			s.Connect();
			var rand = new Random();

			while (!_stopRequested)
			{
				try
				{
					//var request = Guid.NewGuid().ToString();
					//var response = await s.SendAsync<string, string>("TestMessage", request).ConfigureAwait(false);				
					
					var buffer = new byte[1 * 1024 * 1024];
					rand.NextBytes(buffer);

					await s.SendAsync<byte[], EmptyPipeMessage>("RequestOnlyBinary", buffer).ConfigureAwait(false);
				}
				catch (IOException e)
				{
					break;
				}

				Thread.Sleep(100);
			}
		}

		void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			_stopRequested = true;
			_stopEvent.Set();
			e.Cancel = true;
		}
	}
}
