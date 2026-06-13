using DistributedLock.Protocol.ByteOperations;

namespace DistributedLock.Protocol.RWLock;

public readonly ref struct ReleaseRequest : IBSerializable<ReleaseRequest>
{
	public readonly MessageFrame Frame;
	public readonly Guid PermitId;

	public ReleaseRequest(MessageFrame frame, Guid permitId)
	{
		Frame = frame;
		PermitId = permitId;
	}

    public static int RequiredBufferSize => ByteTools.SizeOf<ReleaseRequest>();

    public static ReleaseRequest FromBytes(ReadOnlySpan<byte> bytes)
	{
		var reader = new ByteReader(bytes);
		var frame = reader.ReadMessageFrame();
		var permitId = reader.ReadGuid();

		return new(frame, permitId);
	}

	public ReadOnlySpan<byte> ToBytes(Span<byte> buffer)
	{
		var writer = new ByteWriter(buffer);
		writer.WriteMessageFrame(Frame);
		writer.WriteGuid(PermitId);
		return buffer;		
	}
}
