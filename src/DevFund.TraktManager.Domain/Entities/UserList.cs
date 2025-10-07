using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents metadata for a Trakt user list.
/// </summary>
public sealed class UserList
{
    public UserList(
        string name,
        string? description,
        ListPrivacy privacy,
        Uri? shareLink,
        string type,
        bool displayNumbers,
        bool allowComments,
        string sortBy,
        ListSortOrder sortOrder,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt,
        int itemCount,
        int commentCount,
        int likes,
        ListIds ids,
        ListUser? owner = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(type))
        {
            throw new ArgumentException("Type is required.", nameof(type));
        }

        if (string.IsNullOrWhiteSpace(sortBy))
        {
            throw new ArgumentException("Sort by value is required.", nameof(sortBy));
        }

        if (createdAt == default)
        {
            throw new ArgumentException("Created at timestamp must be specified.", nameof(createdAt));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Updated at timestamp must be specified.", nameof(updatedAt));
        }

        if (itemCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(itemCount), itemCount, "Item count cannot be negative.");
        }

        if (commentCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(commentCount), commentCount, "Comment count cannot be negative.");
        }

        if (likes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(likes), likes, "Likes cannot be negative.");
        }

        Name = name;
        Description = string.IsNullOrWhiteSpace(description) ? null : description;
        Privacy = privacy;
        ShareLink = shareLink;
        Type = type;
        DisplayNumbers = displayNumbers;
        AllowComments = allowComments;
        SortBy = sortBy;
        SortOrder = sortOrder;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        ItemCount = itemCount;
        CommentCount = commentCount;
        Likes = likes;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
        Owner = owner;
    }

    public string Name { get; }

    public string? Description { get; }

    public ListPrivacy Privacy { get; }

    public Uri? ShareLink { get; }

    public string Type { get; }

    public bool DisplayNumbers { get; }

    public bool AllowComments { get; }

    public string SortBy { get; }

    public ListSortOrder SortOrder { get; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset UpdatedAt { get; }

    public int ItemCount { get; }

    public int CommentCount { get; }

    public int Likes { get; }

    public ListIds Ids { get; }

    public ListUser? Owner { get; }
}