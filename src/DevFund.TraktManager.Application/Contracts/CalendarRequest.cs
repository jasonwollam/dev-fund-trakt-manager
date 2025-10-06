namespace DevFund.TraktManager.Application.Contracts;

/// <summary>
/// Request payload for calendar-based use cases.
/// </summary>
public sealed record CalendarRequest
{
    public const int MinDays = 1;
    public const int MaxDays = 120;

    public CalendarRequest(DateOnly startDate, int days)
    {
        if (days is < MinDays or > MaxDays)
        {
            throw new ArgumentOutOfRangeException(nameof(days), days, $"Days must be between {MinDays} and {MaxDays}.");
        }

        StartDate = startDate;
        Days = days;
    }

    public DateOnly StartDate { get; }

    public int Days { get; }

    public DateOnly EndDate => StartDate.AddDays(Days);
}
