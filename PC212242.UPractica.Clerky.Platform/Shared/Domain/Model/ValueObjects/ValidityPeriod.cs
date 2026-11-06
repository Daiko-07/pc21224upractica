namespace PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

public record ValidityPeriod
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }

    public ValidityPeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate <= startDate)
            throw new ArgumentException(
                "End date must be later than start date.");

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool IsExpired(DateOnly checkDate)
    {
        return checkDate > EndDate;
    }
}