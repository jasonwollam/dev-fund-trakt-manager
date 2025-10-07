namespace DevFund.TraktManager.Domain.ValueObjects;

/// <summary>
/// Lightweight representation of a Trakt user associated with a list.
/// </summary>
public sealed record ListUser
{
    public ListUser(string username, bool isPrivate, string? name, bool isVip, bool isVipEp, string slug)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Slug is required.", nameof(slug));
        }

        Username = username;
        IsPrivate = isPrivate;
        Name = string.IsNullOrWhiteSpace(name) ? null : name;
        IsVip = isVip;
        IsVipEp = isVipEp;
        Slug = slug;
    }

    public string Username { get; }

    public bool IsPrivate { get; }

    public string? Name { get; }

    public bool IsVip { get; }

    public bool IsVipEp { get; }

    public string Slug { get; }
}