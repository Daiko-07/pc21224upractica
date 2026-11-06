namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

public record ContractResource(
    int Id,
    string DocumentId,
    string ClientId,
    string PeriodStartDate,
    string PeriodEndDate,
    decimal MonetaryAmountValue,
    string MonetaryAmountCurrency
);