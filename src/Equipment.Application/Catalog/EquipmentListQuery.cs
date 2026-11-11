namespace Equipment.Application.Catalog;

public enum EquipmentSortField
{
    InventoryNumber,
    EquipmentTypeName,
    Name,
    Room
}

public sealed record EquipmentListQuery(
    int Skip,
    int Take,
    EquipmentSortField SortBy,
    bool Descending);
