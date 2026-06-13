namespace DistributedLock.Protocol.RequestHandling;

public sealed class GlobalRequestHandler
{
	private readonly RequestTypeMapper _typeMapper;
	private readonly RequestHandler[] _handlers;

	public GlobalRequestHandler(RequestTypeMapper typeMapper)
	{
		_typeMapper = typeMapper;
		_handlers = new RequestHandler[_typeMapper.RegisteredTypeCount];
	}

	public void RegisterHandler<T>(RequestHandler handler)
		where T : notnull, allows ref struct
	{
		var type = typeof(T);
		var typeIndex = _typeMapper.GetByte(type);
		_handlers[typeIndex] = handler;
	}

	public ReadOnlySpan<byte> HandleRequest(ReadOnlySpan<byte> bytes)
	{
		var handlerIndex = bytes[0];
		var handler = _handlers[handlerIndex];
		return handler(bytes[1..]);
	}
}

public delegate ReadOnlySpan<byte> RequestHandler(
	ReadOnlySpan<byte> request
);
