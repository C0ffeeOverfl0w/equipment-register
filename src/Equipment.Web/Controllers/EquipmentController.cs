namespace Equipment.Web.Controllers;

[ApiController]
[Route("api/equipment")]
public sealed class EquipmentController : ControllerBase
{
    private readonly ISender _sender;

    public EquipmentController(
        ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<EquipmentDto>>> List(
        [FromQuery] int skip = 0,
        [FromQuery] int take = GetEquipmentQuery.DefaultPageSize,
        [FromQuery] string sortBy = EquipmentSorting.InventoryNumber,
        [FromQuery] string sortDirection = EquipmentSorting.Ascending,
        CancellationToken cancellationToken = default)
    {
        var query = new GetEquipmentQuery(skip, take, sortBy, sortDirection);
        Result<PagedResult<EquipmentDto>> result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error, HttpContext);
    }

    [HttpGet("{id:int:min(1)}")]
    public async Task<ActionResult<EquipmentDto>> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        Result<EquipmentDto> result = await _sender.Send(new GetEquipmentByIdQuery(id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error, HttpContext);
    }

    [HttpPost]
    public async Task<ActionResult<EquipmentDto>> Create(
        EquipmentWriteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateEquipmentCommand(request.ToDetails());
        Result<EquipmentDto> result = await _sender.Send(command, cancellationToken);
        if (result.IsFailure)
        {
            return ApiResults.Problem(result.Error, HttpContext);
        }

        return CreatedAtAction(nameof(GetById), new
        {
            id = result.Value.Id
        }, result.Value);
    }

    [HttpPut("{id:int:min(1)}")]
    public async Task<ActionResult<EquipmentDto>> Update(
        int id,
        EquipmentUpdateRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateEquipmentCommand(id, request.ToDetails(), request.Version!.Value);
        Result<EquipmentDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : ApiResults.Problem(result.Error, HttpContext);
    }
}
