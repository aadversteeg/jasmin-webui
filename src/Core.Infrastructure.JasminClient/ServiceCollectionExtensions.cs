using Core.Application.Events;
using Microsoft.Extensions.DependencyInjection;

namespace Core.Infrastructure.JasminClient;

/// <summary>
/// Extension methods for registering JasminClient services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Jasmin client services to the service collection.
    /// </summary>
    public static IServiceCollection AddJasminClient(this IServiceCollection services)
    {
        services.AddScoped<IEventStreamService, EventStreamService>();
        return services;
    }
}
