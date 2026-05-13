using Alerto.Application.Common.Interfaces;

namespace Alerto.Infrastructure.Services;

public sealed class WeatherThresholdStore : IWeatherThresholdStore
{
    private volatile bool _isDemoMode;

    public bool IsDemoMode => _isDemoMode;

    public void SetMode(bool demoMode) => _isDemoMode = demoMode;
}
