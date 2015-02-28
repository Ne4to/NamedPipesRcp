using MsgPack.Serialization;

namespace NamedPipesRcp.MessagePack
{
	public class MessagePackPipeMessageSerializer : IPipeMessageSerializer
	{
		public byte[] Serialize<T>(T data)
		{
			var serializer = SerializationContext.Default.GetSerializer<T>();
			return serializer.PackSingleObject(data);
		}

		public T Deserialize<T>(byte[] buffer)
		{
			var serializer = SerializationContext.Default.GetSerializer<T>();
			return serializer.UnpackSingleObject(buffer);
		}
	}
}