namespace Equipment.Infrastructure.Persistence;

/// <summary>
/// Имена ограничений, общие для конфигурации модели и распознавания конфликтов PostgreSQL
/// </summary>
internal static class DatabaseConstraints
{
    public const string EquipmentInventoryNumberUnique = "uq_equipment_inventory_number";
    public const string EquipmentTypeForeignKey = "fk_equipment_type";
}
