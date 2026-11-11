namespace Equipment.Application.EquipmentItems.GetEquipment;

/// <inheritdoc />
internal sealed class GetEquipmentQueryHandler : IQueryHandler<GetEquipmentQuery, PagedResult<EquipmentDto>>
{
    private readonly IEquipmentReadStore _store;

    public GetEquipmentQueryHandler(
        IEquipmentReadStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentDto>>> Handle(
        GetEquipmentQuery query,
        CancellationToken cancellationToken)
    {
        EquipmentSortField field = query.SortBy switch
        {
            EquipmentSorting.InventoryNumber => EquipmentSortField.InventoryNumber,
            EquipmentSorting.EquipmentTypeName => EquipmentSortField.EquipmentTypeName,
            EquipmentSorting.Name => EquipmentSortField.Name,
            EquipmentSorting.Room => EquipmentSortField.Room,
            _ => throw new InvalidOperationException("A validated sort field is required")
        };

        return await _store.ListAsync(
            new EquipmentListQuery(query.Skip, query.Take, field, query.SortDirection == EquipmentSorting.Descending),
            cancellationToken);
    }
}
