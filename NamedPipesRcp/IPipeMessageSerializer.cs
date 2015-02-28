namespace NamedPipesRcp
{
	public interface IPipeMessageSerializer
	{
		byte[] Serialize<T>(T data);
		T Deserialize<T>(byte[] buffer);
	}
}