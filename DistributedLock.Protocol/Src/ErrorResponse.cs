
using DistributedLock.Protocol.ByteOperations;

namespace DistributedLock.Protocol;

public readonly ref struct ErrorResponse : IBSerializable<ErrorResponse>
{
	public readonly MessageFrame Frame;
	public readonly ErrorState State;

	public ErrorResponse(MessageFrame frame, ErrorState state)
	{
		Frame = frame;
		State = state;
	}

    public static int RequiredBufferSize => ByteTools.SizeOf<ErrorResponse>();

    public static ErrorResponse FromBytes(ReadOnlySpan<byte> bytes)
    {
		var reader = new ByteReader(bytes);
		var frame = reader.ReadMessageFrame();
		var state = (ErrorState)reader.ReadByte();

		return new(frame, state);
    }

    public ReadOnlySpan<byte> ToBytes(Span<byte> buffer)
    {
		var writer = new ByteWriter(buffer);
		writer.WriteMessageFrame(Frame);
		writer.WriteByte((byte)State);
		return buffer;
    }
}
