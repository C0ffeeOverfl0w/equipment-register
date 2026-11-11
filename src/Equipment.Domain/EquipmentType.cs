namespace Equipment.Domain;

public sealed class EquipmentType
{
    private EquipmentType()
    {
    }

    private EquipmentType(
        string name)
    {
        Name = name;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static Result<EquipmentType> Create(
        string name)
    {
        Result validation = EquipmentRules.ValidateName(name, EquipmentRules.TypeNameMaxLength, "name");
        return validation.IsSuccess
            ? new EquipmentType(name)
            : Result.Failure<EquipmentType>(validation.Error);
    }
}
