using Microsoft.AspNetCore.Mvc;
using PC212242.UPractica.Clerky.Platform.Paperwork.Application.Services;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Resources;
using PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Transform;

namespace PC212242.UPractica.Clerky.Platform.Paperwork.Interfaces.REST.Controllers;

/// <summary>
/// Provides REST operations for contract management.
/// </summary>
/// <remarks>
/// Author: Andrea Correa Rodríguez.
/// </remarks>
[ApiController]
[Route("api/v1/contracts")]
public class ContractsController : ControllerBase
{
    private readonly IContractCommandService _contractCommandService;

    public ContractsController(IContractCommandService contractCommandService)
    {
        _contractCommandService = contractCommandService;
    }

    /// <summary>
    /// Creates a new contract.
    /// </summary>
    /// <param name="resource">Contract creation information.</param>
    /// <returns>The created contract.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ContractResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ContractErrorResource), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ContractErrorResource), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateContract(
        [FromBody] CreateContractResource resource)
    {
        try
        {
            var command =
                CreateContractCommandFromResourceAssembler.ToCommandFromResource(resource);

            var contract = await _contractCommandService.Handle(command);

            var contractResource =
                ContractResourceFromEntityAssembler.ToResourceFromEntity(contract);

            return StatusCode(StatusCodes.Status201Created, contractResource);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ContractErrorResource(
                LocalizeMessage(exception)));
        }
        catch (FormatException)
        {
            return BadRequest(new ContractErrorResource(
                IsSpanish()
                    ? "El identificador o la fecha tienen un formato inválido."
                    : "The identifier or date has an invalid format."));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ContractErrorResource(
                LocalizeMessage(exception)));
        }
    }

    private bool IsSpanish()
    {
        var language = Request.Headers.AcceptLanguage.ToString();

        return language.StartsWith(
            "es",
            StringComparison.OrdinalIgnoreCase);
    }

    private string LocalizeMessage(Exception exception)
    {
        if (!IsSpanish())
            return exception.Message;

        if (exception is ArgumentOutOfRangeException)
            return "El monto monetario no puede ser negativo.";

        if (exception is ArgumentNullException)
            return "La moneda no puede estar vacía.";

        return exception.Message switch
        {
            "End date must be later than start date." =>
                "La fecha de finalización debe ser posterior a la fecha de inicio.",

            "Document identifier cannot be empty." =>
                "El identificador del documento no puede estar vacío.",

            "Client identifier cannot be empty." =>
                "El identificador del cliente no puede estar vacío.",

            "A contract with this document identifier already exists." =>
                "Ya existe un contrato con este identificador de documento.",

            _ => exception.Message
        };
    }
}
