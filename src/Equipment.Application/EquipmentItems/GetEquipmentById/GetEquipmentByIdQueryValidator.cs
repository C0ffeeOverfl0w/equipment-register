namespace Equipment.Application.EquipmentItems.GetEquipmentById;

internal sealed class GetEquipmentByIdQueryValidator : AbstractValidator<GetEquipmentByIdQuery>
{
    public GetEquipmentByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .GreaterThan(0)
            .WithMessage("Укажите положительный идентификатор карточки.")
            .OverridePropertyName("id");
    }
}
