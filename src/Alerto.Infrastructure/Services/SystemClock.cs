using Alerto.Application.Common.Interfaces;

namespace Alerto.Infrastructure.Services;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
