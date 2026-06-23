namespace DistributedLock.Protocol;

public interface IBSerializable<TSelf>
	where TSelf : notnull, IBSerializable<TSelf>, allows ref struct
{
	static abstract int MaxByteSize { get; }
	static abstract int MinByteSize { get; }
	static abstract TSelf FromBytes(ReadOnlyMemory<byte> bytes);

	ReadOnlyMemory<byte> ToBytes(Memory<byte> buffer);
}
