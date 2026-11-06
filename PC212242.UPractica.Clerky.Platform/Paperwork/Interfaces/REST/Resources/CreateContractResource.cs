namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

/// <summary>
/// Represents the information required to create a contract.
/// </summary>
/// <remarks>
/// Author: Andrea Correa Rodríguez.
/// </remarks>
public record CreateContractResource(
    string DocumentId,
    string ClientId,
    string PeriodStartDate,
    string PeriodEndDate,
    decimal MonetaryAmountValue,
    string MonetaryAmountCurrency,
    string? Notes = null
);