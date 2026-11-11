namespace Equipment.Application.Commands;

/// <summary>
/// Проверяет входные поля по общим правилам предметной модели
/// </summary>
internal sealed class EquipmentDetailsValidator : AbstractValidator<EquipmentDetails>
{
    public EquipmentDetailsValidator()
    {
        RuleFor(details => details.InventoryNumber)
            .Must(EquipmentRules.IsValidInventoryNumber)
            .WithMessage(EquipmentErrors.InvalidInventoryNumber.Description)
            .OverridePropertyName("inventoryNumber");

        RuleFor(details => details.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("Укажите наименование.")
            .Must(name => EquipmentRules.IsValidName(name, EquipmentRules.NameMaxLength))
            .WithMessage($"Допустимо не более {EquipmentRules.NameMaxLength} символов без нулевого символа.")
            .OverridePropertyName("name");

        RuleFor(details => details.EquipmentTypeId)
            .GreaterThan(0)
            .WithMessage(EquipmentErrors.InvalidType.Description)
            .OverridePropertyName(EquipmentErrors.InvalidType.Field!);

        RuleFor(details => details.Room)
            .InclusiveBetween(EquipmentRules.MinimumRoom, EquipmentRules.MaximumRoom)
            .WithMessage(EquipmentErrors.InvalidRoom.Description)
            .OverridePropertyName(EquipmentErrors.InvalidRoom.Field!);
    }
}
