using Alerto.Application.Alerts;
using Alerto.Application.Auth;
using Alerto.Application.Geofences;
using Alerto.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Alerto.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(typeof(DependencyInjection).Assembly);
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAlertService, AlertService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IGeofenceService, GeofenceService>();
        services.AddScoped<IUserService, UserService>();

        return services;
    }
}
