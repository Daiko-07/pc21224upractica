using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

public static class ContractResourceFromEntityAssembler
{
    public static ContractResource ToResourceFromEntity(Contract contract)
    {
        return new ContractResource(
            contract.Id,
            contract.DocumentIdentifier.Value.ToString(),
            contract.ClientId.Value.ToString(),
            contract.Period.StartDate.ToString("yyyy-MM-dd"),
            contract.Period.EndDate.ToString("yyyy-MM-dd"),
            contract.TotalValue.Amount,
            contract.TotalValue.Currency
        );
    }
}