namespace DistributedLock.Protocol;

public enum ErrorState : byte
{
	Unexpected,
	PermitNotFound,
	BadRequest
}
