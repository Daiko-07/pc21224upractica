using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Repositories;

public interface IContractRepository
{
    Task AddAsync(Contract contract);
    Task<bool> ExistsByDocumentIdentifierAsync(Guid documentIdentifier);
}