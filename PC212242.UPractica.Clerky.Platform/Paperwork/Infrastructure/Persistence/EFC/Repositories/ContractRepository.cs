using Microsoft.EntityFrameworkCore;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;
using PC212242.UPractica.Clerky.Platform.Shared.Infrastructure.Persistence.EFC;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Infrastructure.Persistence.EFC.Repositories;

public class ContractRepository(AppDbContext context) : IContractRepository
{
    public async Task AddAsync(Contract contract)
    {
        await context.Contracts.AddAsync(contract);
        await context.SaveChangesAsync();
    }

    public async Task<bool> ExistsByDocumentIdentifierAsync(Guid documentIdentifier)
    {
        return await context.Contracts.AnyAsync(
            contract => contract.DocumentIdentifier.Value == documentIdentifier);
    }
}