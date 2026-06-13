using DistributedLock.Protocol.ByteOperations;

namespace DistributedLock.Protocol;

public readonly ref struct MessageFrame
{
	public readonly Guid RequestId;

	public MessageFrame(Guid requestId)
	{
		RequestId = requestId;
	}

}
