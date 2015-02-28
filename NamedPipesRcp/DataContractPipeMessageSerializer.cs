using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace NamedPipesRcp
{
	public class DataContractPipeMessageSerializer : IPipeMessageSerializer
	{
		private readonly ICollection<Type> _knownTypes;

		public DataContractPipeMessageSerializer(ICollection<Type> knownTypes = null)
		{
			_knownTypes = knownTypes;
		}

		public byte[] Serialize<T>(T data)
		{
			var serializer = GetSerializer<T>();
			using (var stream = new MemoryStream())
			{
				serializer.WriteObject(stream, data);
				return stream.ToArray();
			}
		}

		public T Deserialize<T>(byte[] buffer)
		{
			var serializer = GetSerializer<T>();
			using (var stream = new MemoryStream(buffer))
			{
				return (T)serializer.ReadObject(stream);
			}
		}

		private DataContractSerializer GetSerializer<T>()
		{
			if (_knownTypes == null)
				return new DataContractSerializer(typeof(T));

			return new DataContractSerializer(typeof(T), _knownTypes);
		}
	}
}