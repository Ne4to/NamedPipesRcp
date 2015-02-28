using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using NamedPipesRcp;
using System.Threading.Tasks;

namespace DemoClient
{
	class Program
	{
		readonly ManualResetEvent _stopEvent = new ManualResetEvent(false);

		static void Main(string[] args)
		{
			Program p = new Program();
			p.Start();
		}

		private void Start()
		{
			Console.CancelKeyPress += Console_CancelKeyPress;
			Console.WriteLine("Press Ctrl-C to exit");

			DoWork();
			
			_stopEvent.WaitOne();
		}

		private async void DoWork()
		{
			//var serializer = new BinaryFormatterPipeMessageSerializer();
			var serializer = new DataContractPipeMessageSerializer();
			var s = new PipeClient(".", "demoPipe", serializer);

			s.Connect();

			while (true)
			{
				var userInput = Console.ReadLine();
				if (userInput == null)
					break;

				try
				{
					var response = await s.SendAsync<string, string>("TestMessage", userInput).ConfigureAwait(false);
					Console.WriteLine(response);
				}
				catch (IOException e)
				{
					Console.WriteLine(e);
					break;
				}
			}		
		}

		void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
		{
			_stopEvent.Set();
			e.Cancel = true;
		}
	}
}
