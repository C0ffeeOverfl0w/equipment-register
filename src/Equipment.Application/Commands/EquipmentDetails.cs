namespace Equipment.Application.Commands;

public sealed record EquipmentDetails(
    string InventoryNumber,
    string Name,
    int EquipmentTypeId,
    int Room);
