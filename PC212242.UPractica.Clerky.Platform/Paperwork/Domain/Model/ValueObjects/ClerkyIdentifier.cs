namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;

public record ClerkyIdentifier
{
    public Guid Value { get; init; }

    public ClerkyIdentifier(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Document identifier cannot be empty.");

        Value = value;
    }
}