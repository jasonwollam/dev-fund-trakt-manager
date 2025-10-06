namespace DevFund.TraktManager.Infrastructure.Options;

public sealed class TraktOptions
{
    public const string SectionName = "Trakt";

    public string? BaseAddress { get; set; } = "https://api.trakt.tv";

    public string? ClientId { get; set; }

    public string? ClientSecret { get; set; }

    public string? AccessToken { get; set; }

    public string? RefreshToken { get; set; }
}
