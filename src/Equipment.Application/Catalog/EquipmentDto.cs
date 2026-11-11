namespace Equipment.Application.Catalog;

public sealed record EquipmentDto(
    int Id,
    string InventoryNumber,
    string Name,
    int EquipmentTypeId,
    string EquipmentTypeName,
    int Room,
    long Version);

public sealed record EquipmentTypeDto(
    int Id,
    string Name);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount);
