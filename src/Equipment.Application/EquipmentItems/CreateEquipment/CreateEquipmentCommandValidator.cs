namespace Equipment.Application.EquipmentItems.CreateEquipment;

internal sealed class CreateEquipmentCommandValidator : AbstractValidator<CreateEquipmentCommand>
{
    public CreateEquipmentCommandValidator()
    {
        RuleFor(command => command.Details)
            .NotNull()
            .WithMessage("Передайте данные карточки.")
            .OverridePropertyName("details");

        RuleFor(command => command.Details)
            .SetValidator(new EquipmentDetailsValidator())
            .OverridePropertyName(string.Empty);
    }
}
