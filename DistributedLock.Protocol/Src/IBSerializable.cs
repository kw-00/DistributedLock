namespace DistributedLock.Protocol;

public interface IBSerializable<TSelf>
	where TSelf : notnull, IBSerializable<TSelf>, allows ref struct
{
	static abstract int RequiredBufferSize { get; }
	static abstract TSelf FromBytes(ReadOnlySpan<byte> bytes);

	ReadOnlySpan<byte> ToBytes(Span<byte> buffer);
}
