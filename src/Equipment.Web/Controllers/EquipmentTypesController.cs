namespace Equipment.Web.Controllers;

[ApiController]
[Route("api/equipment-types")]
public sealed class EquipmentTypesController : ControllerBase
{
    private readonly ISender _sender;

    public EquipmentTypesController(
        ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentTypeDto>>> List(
        CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<EquipmentTypeDto>> result = await _sender.Send(new GetEquipmentTypesQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error, HttpContext);
    }
}
