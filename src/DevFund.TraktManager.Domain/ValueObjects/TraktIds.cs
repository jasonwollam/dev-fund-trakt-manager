namespace DevFund.TraktManager.Domain.ValueObjects;

/// <summary>
/// Strongly typed identifier set for items returned by the Trakt API.
/// </summary>
public sealed record TraktIds
{
    public TraktIds(int trakt, string slug, string? imdb = null, int? tmdb = null)
    {
        if (trakt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trakt), trakt, "Trakt ids must be positive.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        Trakt = trakt;
        Slug = slug;
        Imdb = imdb;
        Tmdb = tmdb;
    }

    public int Trakt { get; }

    public string Slug { get; }

    public string? Imdb { get; }

    public int? Tmdb { get; }

    public bool HasImdb => !string.IsNullOrWhiteSpace(Imdb);

    public bool HasTmdb => Tmdb.HasValue;
}
