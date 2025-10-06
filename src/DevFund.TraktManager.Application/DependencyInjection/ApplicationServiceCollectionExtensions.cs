using DevFund.TraktManager.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevFund.TraktManager.Application.DependencyInjection;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationLayer(this IServiceCollection services)
    {
        services.AddScoped<CalendarService>();
        services.AddScoped<CalendarOrchestrator>();
        return services;
    }
}
