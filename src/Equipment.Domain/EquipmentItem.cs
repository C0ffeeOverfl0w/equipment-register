namespace Equipment.Domain;

/// <summary>
/// Карточка техники с локальными инвариантами и версией конкурентного редактирования
/// </summary>
public sealed class EquipmentItem
{
    private EquipmentItem()
    {
    }

    private EquipmentItem(
        string inventoryNumber,
        string name,
        int equipmentTypeId,
        int room)
    {
        InventoryNumber = inventoryNumber;
        Name = name;
        EquipmentTypeId = equipmentTypeId;
        Room = room;
    }

    public int Id { get; private set; }
    public string InventoryNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int EquipmentTypeId { get; private set; }
    public EquipmentType Type { get; private set; } = null!;
    public int Room { get; private set; }
    public long Version { get; private set; } = 1;

    /// <summary>
    /// Создаёт корректную карточку с начальной версией без обращения к хранилищу
    /// </summary>
    public static Result<EquipmentItem> Create(
        string inventoryNumber,
        string name,
        int equipmentTypeId,
        int room)
    {
        Result validation = ValidateDetails(inventoryNumber, name, equipmentTypeId, room);
        if (validation.IsFailure)
        {
            return Result.Failure<EquipmentItem>(validation.Error);
        }

        return new EquipmentItem(inventoryNumber, name, equipmentTypeId, room);
    }

    /// <summary>
    /// Проверяет все поля до изменения карточки и увеличивает версию при успехе
    /// </summary>
    public Result UpdateDetails(
        string inventoryNumber,
        string name,
        int equipmentTypeId,
        int room)
    {
        Result validation = ValidateDetails(inventoryNumber, name, equipmentTypeId, room);
        if (validation.IsFailure)
        {
            return validation;
        }

        long nextVersion = checked(Version + 1);
        InventoryNumber = inventoryNumber;
        Name = name;
        EquipmentTypeId = equipmentTypeId;
        Room = room;
        Version = nextVersion;
        return Result.Success();
    }

    private static Result ValidateDetails(
        string inventoryNumber,
        string name,
        int equipmentTypeId,
        int room)
    {
        if (!EquipmentRules.IsValidInventoryNumber(inventoryNumber))
        {
            return Result.Failure(EquipmentErrors.InvalidInventoryNumber);
        }

        Result nameValidation = EquipmentRules.ValidateName(name, EquipmentRules.NameMaxLength, "name");
        if (nameValidation.IsFailure)
        {
            return nameValidation;
        }

        if (equipmentTypeId <= 0)
        {
            return Result.Failure(EquipmentErrors.InvalidType);
        }

        if (room is < EquipmentRules.MinimumRoom or > EquipmentRules.MaximumRoom)
        {
            return Result.Failure(EquipmentErrors.InvalidRoom);
        }

        return Result.Success();
    }
}
