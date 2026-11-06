using System.Globalization;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

/// <summary>
/// Converts contract creation resources into commands.
/// </summary>
/// <remarks>
/// Author: Andrea Correa Rodríguez.
/// </remarks>
public static class CreateContractCommandFromResourceAssembler
{
    /// <summary>
    /// Creates a contract command from a REST resource.
    /// </summary>
    public static CreateContractCommand ToCommandFromResource(
        CreateContractResource resource)
    {
        return new CreateContractCommand(
            Guid.Parse(resource.DocumentId),
            Guid.Parse(resource.ClientId),
            DateOnly.ParseExact(
                resource.PeriodStartDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            DateOnly.ParseExact(
                resource.PeriodEndDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture),
            resource.MonetaryAmountValue,
            resource.MonetaryAmountCurrency,
            resource.Notes
        );
    }
}