using DevFund.TraktManager.Domain.ValueObjects;

namespace DevFund.TraktManager.Domain.Entities;

/// <summary>
/// Represents a partial season object returned in list-centric responses.
/// </summary>
public sealed class SeasonSummary
{
    public SeasonSummary(int number, SeasonIds ids)
    {
        if (number < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), number, "Season number must be zero or positive.");
        }

        Number = number;
        Ids = ids ?? throw new ArgumentNullException(nameof(ids));
    }

    public int Number { get; }

    public SeasonIds Ids { get; }

    public override string ToString() => $"Season {Number}";
}
