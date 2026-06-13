namespace DistributedLock.Protocol.RequestHandling;

public sealed class RequestTypeMapper
{
	public const int MaxMappedTypes = byte.MaxValue + 1;
	public readonly int RegisteredTypeCount;
	private readonly Type[] _byteToType;
	private readonly Dictionary<Type, byte> _typeToByte = new();

	public RequestTypeMapper()
	{
		var types = GetType()
			.Assembly
			.GetTypes()
			.Where(
				t => t.IsDefined(typeof(RequestAttribute), false)
			)
			.ToArray();
		if (types.Length  > MaxMappedTypes)
			throw new Exception(
				$"Up to {MaxMappedTypes} request types can be ."
			);
		_byteToType = types;
		RegisteredTypeCount = types.Length;
		byte currentIndex = 0;
		foreach (var type in types)
		{
			_byteToType[currentIndex] = type;
			_typeToByte[type] = currentIndex;
			if (currentIndex < byte.MaxValue)
				currentIndex++;
		}
	}

	public Type GetType(byte b)
	{
		try
		{
			return _byteToType[b];
		}
		catch (IndexOutOfRangeException ex)
		{
			throw new Exception($"No type registered under byte of {b}.", ex);
		}
	}

	public byte GetByte(Type t)
	{
		try
		{
			return _typeToByte[t];
		}
		catch (KeyNotFoundException ex)
		{
			throw new Exception($"Type {t} is not registered.", ex);
		}
	}
}


