namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;

public record CreateContractCommand(
    Guid DocumentIdentifier,
    Guid ClientId,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Amount,
    string Currency,
    string? Notes
);