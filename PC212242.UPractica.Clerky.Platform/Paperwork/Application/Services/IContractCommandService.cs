using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Aggregates;
using PC212242.UPractica.Clerky.Platform.Paperwork.Domain.Model.Commands;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;

public interface IContractCommandService
{
    Task<Contract> Handle(CreateContractCommand command);
}