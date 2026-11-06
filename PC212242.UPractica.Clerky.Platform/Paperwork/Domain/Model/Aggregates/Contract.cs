using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Enumerations;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.ValueObjects;
using PC212242.UPractica.Clerky.Platform.Shared.Domain.Model.ValueObjects;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;

public class Contract
{
    public int Id { get; private set; }

    public ClerkyIdentifier DocumentIdentifier { get; private set; }

    public PartyId ClientId { get; private set; }

    public ValidityPeriod Period { get; private set; }

    public MonetaryAmount TotalValue { get; private set; }

    public ContractStatus Status { get; private set; }

    public string? Notes { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime UpdatedAt { get; private set; }

    private Contract()
    {
        DocumentIdentifier = null!;
        ClientId = null!;
        Period = null!;
        TotalValue = null!;
    }

    public Contract(
        Guid documentIdentifier,
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        decimal amount,
        string currency,
        string? notes)
    {
        DocumentIdentifier = new ClerkyIdentifier(documentIdentifier);
        ClientId = new PartyId(clientId);
        Period = new ValidityPeriod(startDate, endDate);
        TotalValue = new MonetaryAmount(amount, currency);
        Status = ContractStatus.Draft;
        Notes = notes;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}