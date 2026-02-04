using Core.Application.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.LocalStorage;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddLocalStorage(this IServiceCollection services)
    {
        services.AddScoped<ILocalStorageService, LocalStorageService>();
        services.AddScoped<IUserPreferencesService, UserPreferencesService>();
        services.AddScoped<IApplicationStateService, ApplicationStateService>();
        return services;
    }
}
