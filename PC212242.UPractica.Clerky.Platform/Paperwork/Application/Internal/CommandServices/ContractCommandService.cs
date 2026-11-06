using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Application.Internal.CommandServices;

public class ContractCommandService(
    IContractRepository contractRepository
) : IContractCommandService
{
    public async Task<Contract> Handle(CreateContractCommand command)
    {
        var exists = await contractRepository
            .ExistsByDocumentIdentifierAsync(command.DocumentIdentifier);

        if (exists)
            throw new InvalidOperationException(
                "A contract with this document identifier already exists.");

        var contract = new Contract(
            command.DocumentIdentifier,
            command.ClientId,
            command.StartDate,
            command.EndDate,
            command.Amount,
            command.Currency,
            command.Notes
        );

        await contractRepository.AddAsync(contract);

        return contract;
    }
}