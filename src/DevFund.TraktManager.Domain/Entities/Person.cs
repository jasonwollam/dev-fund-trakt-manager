using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents a person resource returned by the Trakt API.
/// </summary>
public sealed class Person
{
    public Person(string name, TraktIds ids)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = name;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public string Name { get; }

    public TraktIds Ids { get; }

    public override string ToString() => Name;
}