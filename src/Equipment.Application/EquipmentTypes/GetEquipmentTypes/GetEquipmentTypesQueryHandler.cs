namespace Equipment.Application.EquipmentTypes.GetEquipmentTypes;

internal sealed class GetEquipmentTypesQueryHandler : IQueryHandler<GetEquipmentTypesQuery, IReadOnlyList<EquipmentTypeDto>>
{
    private readonly IEquipmentReadStore _store;

    public GetEquipmentTypesQueryHandler(
        IEquipmentReadStore store)
    {
        _store = store;
    }

    public async Task<Result<IReadOnlyList<EquipmentTypeDto>>> Handle(
        GetEquipmentTypesQuery query,
        CancellationToken cancellationToken)
    {
        return Result.Success(await _store.GetTypesAsync(cancellationToken));
    }
}
