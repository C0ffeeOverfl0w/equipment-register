namespace Equipment.Application.EquipmentItems.GetEquipmentById;

/// <inheritdoc />
internal sealed class GetEquipmentByIdQueryHandler : IQueryHandler<GetEquipmentByIdQuery, EquipmentDto>
{
    private readonly IEquipmentReadStore _store;

    public GetEquipmentByIdQueryHandler(
        IEquipmentReadStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentDto>> Handle(
        GetEquipmentByIdQuery query,
        CancellationToken cancellationToken)
    {
        EquipmentDto? item = await _store.GetByIdAsync(query.Id, cancellationToken);
        return item is not null ? item : Result.Failure<EquipmentDto>(EquipmentErrors.NotFound);
    }
}
