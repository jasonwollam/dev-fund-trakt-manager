namespace DevFund.TraktManager.Domain.ValueObjects;

/// <summary>
/// Identifier set for Trakt user lists.
/// </summary>
public sealed record ListIds
{
    public ListIds(int trakt, string slug)
    {
        if (trakt <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(trakt), trakt, "Trakt id must be positive.");
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        Trakt = trakt;
        Slug = slug;
    }

    public int Trakt { get; }

    public string Slug { get; }
}