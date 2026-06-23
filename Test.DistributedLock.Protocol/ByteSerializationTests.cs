using DistributedLock.Protocol;
using DistributedLock.Protocol.RWLock;
using Xunit.Abstractions;

namespace Test.DistributedLock.Protocol;

public abstract class ByteSerializationTest<T>
	where T : notnull, IBSerializable<T>, allows ref struct
{

	private ITestOutputHelper _output;

	public ByteSerializationTest(ITestOutputHelper output)
	{
		_output = output;
	}
	
	[Fact]
	public void SerializationRoundtrip_ShouldNotChangeStructure()
	{
		for (int i = 0; i < 50; i++)
		{
			var x = CreateSerializable(new Random(i));
			var xBuffer = new byte[T.RequiredBufferSize];
			x = T.FromBytes(x.ToBytes(xBuffer));
			var xBytes = x.ToBytes(xBuffer);
			var y = CreateSerializable(new Random(i));
			var yBuffer = new byte[T.RequiredBufferSize];
			var yBytes = y.ToBytes(yBuffer);
			_output.WriteLine("X bytes: {0}", xBytes.ToArray());
			_output.WriteLine("Y bytes: {0}", yBytes.ToArray());
			Assert.True(xBuffer.SequenceEqual(yBuffer));
		}
	}

	public static Guid CreateGuid(Random random)
	{
		var buffer = new byte[16];
		random.NextBytes(buffer);
		return new Guid(buffer);
	}

	public static MessageFrame CreateMessageFrame(Random random)
	{
		return new MessageFrame(CreateGuid(random));
	}

	protected abstract T CreateSerializable(Random random);
	
}

public class AcquireRequestSerializationTest(ITestOutputHelper outputHelper)
	: ByteSerializationTest<AcquireRequest>(outputHelper)
{
	protected override AcquireRequest CreateSerializable(Random random)
	{
		var messageFrame = CreateMessageFrame(random);
		var clientId = CreateGuid(random);
		var resourceHash = random.Next();
		return new(messageFrame, clientId, resourceHash);
	}
}
public class ReleaseRequestSerializationTest(ITestOutputHelper outputHelper)
	: ByteSerializationTest<ReleaseRequest>(outputHelper)
{
	protected override ReleaseRequest CreateSerializable(Random random)
	{
		var messageFrame = CreateMessageFrame(random);
		var permitId = CreateGuid(random);
		return new(messageFrame, permitId);
	}
}

public class AcquiredResponseSerializationTest(ITestOutputHelper outputHelper)
	: ByteSerializationTest<AcquiredResponse>(outputHelper)
{
	protected override AcquiredResponse CreateSerializable(Random random)
	{
		var messageFrame = CreateMessageFrame(random);
		var permitId = CreateGuid(random);
		return new(messageFrame, permitId);
	}
}

public class ErrorResponseSerializationTest(ITestOutputHelper outputHelper)
	: ByteSerializationTest<ErrorResponse>(outputHelper)
{
	protected override ErrorResponse CreateSerializable(Random random)
	{
		var messageFrame = CreateMessageFrame(random);
		var errorState = (ErrorState)(byte)(random.Next() % 3);
		return new(messageFrame, errorState);
	}
}
