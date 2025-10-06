using System.Linq;
using System.Net.Http.Headers;
using DevFund.TraktManager.Application.Abstractions;
using DevFund.TraktManager.Infrastructure.Http;
using DevFund.TraktManager.Infrastructure.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace DevFund.TraktManager.Infrastructure.DependencyInjection;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<TraktOptions>()
            .Bind(configuration.GetSection(TraktOptions.SectionName));

        services.AddSingleton<ITraktAccessTokenStore, InMemoryTraktAccessTokenStore>();

        services.AddHttpClient<ITraktDeviceAuthClient, TraktDeviceAuthClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TraktOptions>>().Value;

            if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var baseAddress))
            {
                baseAddress = new Uri("https://api.trakt.tv", UriKind.Absolute);
            }

            client.BaseAddress = baseAddress;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!client.DefaultRequestHeaders.UserAgent.Any())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DevFund-TraktManager/1.0");
            }
        });

        services.AddHttpClient<ITraktCalendarClient, TraktCalendarClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TraktOptions>>().Value;

            if (!Uri.TryCreate(options.BaseAddress, UriKind.Absolute, out var baseAddress))
            {
                baseAddress = new Uri("https://api.trakt.tv", UriKind.Absolute);
            }

            client.BaseAddress = baseAddress;
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-version", "2");

            if (string.IsNullOrWhiteSpace(options.ClientId))
            {
                throw new InvalidOperationException("Trakt:ClientId must be configured.");
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation("trakt-api-key", options.ClientId);

            // Provide a predictable User-Agent to satisfy Trakt requirements.
            if (!client.DefaultRequestHeaders.UserAgent.Any())
            {
                client.DefaultRequestHeaders.UserAgent.ParseAdd("DevFund-TraktManager/1.0");
            }
        });

        return services;
    }
}
