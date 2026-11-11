namespace Equipment.Domain;

public static class EquipmentErrors
{
    public static readonly Error InvalidInventoryNumber = new(
        "Equipment.InvalidInventoryNumber", "Укажите от 1 до 32 латинских букв и цифр.", ErrorType.Validation, "inventoryNumber");

    public static readonly Error InvalidType = new(
        "Equipment.InvalidType", "Выберите тип техники.", ErrorType.Validation, "equipmentTypeId");

    public static readonly Error UnknownType = new(
        "Equipment.UnknownType", "Выбранный тип техники не существует. Обновите справочник.", ErrorType.Validation, "equipmentTypeId");

    public static readonly Error InvalidRoom = new(
        "Equipment.InvalidRoom", "Укажите целое число от 1 до 1000.", ErrorType.Validation, "room");

    public static readonly Error NotFound = new(
        "equipment_not_found", "Карточка техники не найдена.", ErrorType.NotFound);

    public static readonly Error InventoryNumberConflict = new(
        "inventory_number_conflict", "Карточка с таким учётным номером уже существует.", ErrorType.Conflict, "inventoryNumber");

    public static readonly Error LimitReached = new(
        "equipment_limit_reached", "Достигнут предел 2000 единиц техники.", ErrorType.Conflict);

    public static readonly Error VersionConflict = new(
        "equipment_version_conflict", "Карточка изменена другим пользователем. Перечитайте её перед сохранением.", ErrorType.Conflict);
}
