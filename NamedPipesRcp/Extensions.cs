using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NamedPipesRcp
{
	public static class Extensions
	{
		public static Task WaitForConnectionAsync(this NamedPipeServerStream stream)
		{
			return Task.Factory.FromAsync(stream.BeginWaitForConnection, stream.EndWaitForConnection, null);			
		}
	}
}
