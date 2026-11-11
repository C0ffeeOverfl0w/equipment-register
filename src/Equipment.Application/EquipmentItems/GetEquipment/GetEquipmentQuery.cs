namespace Equipment.Application.EquipmentItems.GetEquipment;

public sealed record GetEquipmentQuery(
    int Skip,
    int Take,
    string? SortBy,
    string? SortDirection)
    : IQuery<PagedResult<EquipmentDto>>
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
}
