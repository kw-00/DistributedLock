namespace DistributedLock.Protocol;

public interface ISized
{
	static abstract int SizeInBytes { get; }
}
