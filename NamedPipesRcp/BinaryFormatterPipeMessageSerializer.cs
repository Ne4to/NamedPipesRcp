using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NamedPipesRcp
{
	public class BinaryFormatterPipeMessageSerializer : IPipeMessageSerializer
	{
		readonly BinaryFormatter _formatter = new BinaryFormatter();

		public byte[] Serialize<T>(T data)
		{
			using (var stream = new MemoryStream())
			{
				_formatter.Serialize(stream, data);
				return stream.ToArray();
			}
		}

		public T Deserialize<T>(byte[] buffer)
		{
			using (var stream = new MemoryStream(buffer))
			{
				return (T)_formatter.Deserialize(stream);
			}			
		}
	}
}