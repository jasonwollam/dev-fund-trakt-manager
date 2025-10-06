namespace DevFund.TraktManager.Domain.ValueObjects;

/// <summary>
/// Identifier set for seasons returned by the Trakt API. Not all identifiers are guaranteed to be present.
/// </summary>
public sealed record SeasonIds
{
    public SeasonIds(int? trakt, int? tvdb, int? tmdb)
    {
        if (trakt is not null && trakt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trakt), trakt, "Trakt id must be positive when provided.");
        }

        if (tvdb is not null && tvdb <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tvdb), tvdb, "TVDB id must be positive when provided.");
        }

        if (tmdb is not null && tmdb <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tmdb), tmdb, "TMDB id must be positive when provided.");
        }

        if (trakt is null && tvdb is null && tmdb is null)
        {
            throw new ArgumentException("At least one season identifier must be provided.", nameof(trakt));
        }

        Trakt = trakt;
        Tvdb = tvdb;
        Tmdb = tmdb;
    }

    public int? Trakt { get; }

    public int? Tvdb { get; }

    public int? Tmdb { get; }

    public bool HasTrakt => Trakt is not null;
    public bool HasTvdb => Tvdb is not null;
    public bool HasTmdb => Tmdb is not null;
}
