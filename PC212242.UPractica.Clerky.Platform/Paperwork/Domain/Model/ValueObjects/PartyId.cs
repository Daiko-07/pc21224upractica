namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;

public record PartyId
{
    public Guid Value { get; init; }

    public PartyId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Client identifier cannot be empty.");

        Value = value;
    }
}