namespace Alerto.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string Username { get; }
    bool IsAuthenticated { get; }
    string ClientIp { get; }
    string TraceId { get; }
}
