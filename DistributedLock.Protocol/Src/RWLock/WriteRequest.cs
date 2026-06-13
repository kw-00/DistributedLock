using DistributedLock.Protocol.ByteOperations;

namespace DistributedLock.Protocol.RWLock;

public readonly ref struct WriteRequest : IBSerializable<WriteRequest>
{
	public readonly MessageFrame Frame;
	public readonly Guid ClientId;
	public readonly int ResourceHash;

	public WriteRequest(MessageFrame frame, Guid clientId, int resourceHash)
	{
		Frame = frame;
		ClientId = clientId;
		ResourceHash = resourceHash;
	}

    public static int RequiredBufferSize => ByteTools.SizeOf<WriteRequest>();

    public static WriteRequest FromBytes(ReadOnlySpan<byte> bytes)
	{
		var reader = new ByteReader(bytes);
		var frame = reader.ReadMessageFrame();
		var clientId = reader.ReadGuid();
		var resourceHash = reader.ReadInt();

		return new(frame, clientId, resourceHash);
	}

	public ReadOnlySpan<byte> ToBytes(Span<byte> buffer)
	{
		var writer = new ByteWriter(buffer);
		writer.WriteMessageFrame(Frame);
		writer.WriteGuid(ClientId);
		writer.WriteInt(ResourceHash);
		return buffer;
	}
}
