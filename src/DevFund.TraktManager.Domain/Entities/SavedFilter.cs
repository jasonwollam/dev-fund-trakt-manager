using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents a saved filter owned by the authenticated user.
/// </summary>
public sealed class SavedFilter
{
    public SavedFilter(int rank, int id, SavedFilterSection section, string name, string path, string query, DateTimeOffset updatedAt)
    {
        if (rank <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rank), rank, "Rank must be positive.");
        }

        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), id, "Identifier must be positive.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Path is required.", nameof(path));
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            throw new ArgumentException("Query is required.", nameof(query));
        }

        if (updatedAt == default)
        {
            throw new ArgumentException("Updated timestamp must be specified.", nameof(updatedAt));
        }

        Rank = rank;
        Id = id;
        Section = section;
        Name = name;
        Path = path;
        Query = query;
        UpdatedAt = updatedAt;
    }

    public int Rank { get; }

    public int Id { get; }

    public SavedFilterSection Section { get; }

    public string Name { get; }

    public string Path { get; }

    public string Query { get; }

    public DateTimeOffset UpdatedAt { get; }
}