namespace Equipment.Application.EquipmentItems.UpdateEquipment;

internal sealed class UpdateEquipmentCommandValidator : AbstractValidator<UpdateEquipmentCommand>
{
    public UpdateEquipmentCommandValidator()
    {
        RuleFor(command => command.Id)
            .GreaterThan(0)
            .WithMessage("Укажите положительный идентификатор карточки.")
            .OverridePropertyName("id");

        RuleFor(command => command.ExpectedVersion)
            .GreaterThan(0)
            .WithMessage("Передайте исходную положительную версию карточки.")
            .OverridePropertyName("version");

        RuleFor(command => command.Details)
            .NotNull()
            .WithMessage("Передайте данные карточки.")
            .OverridePropertyName("details");

        RuleFor(command => command.Details)
            .SetValidator(new EquipmentDetailsValidator())
            .OverridePropertyName(string.Empty);
    }
}
