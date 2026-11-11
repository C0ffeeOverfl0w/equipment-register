namespace Equipment.Application.EquipmentItems.GetEquipment;

internal sealed class GetEquipmentQueryValidator : AbstractValidator<GetEquipmentQuery>
{
    public GetEquipmentQueryValidator()
    {
        RuleFor(query => query.Skip)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Смещение не может быть отрицательным.")
            .OverridePropertyName("skip");

        RuleFor(query => query.Take)
            .InclusiveBetween(1, GetEquipmentQuery.MaximumPageSize)
            .WithMessage($"Размер страницы должен быть от 1 до {GetEquipmentQuery.MaximumPageSize}.")
            .OverridePropertyName("take");

        RuleFor(query => query.SortBy)
            .Must(value => value is EquipmentSorting.InventoryNumber or EquipmentSorting.EquipmentTypeName
                or EquipmentSorting.Name or EquipmentSorting.Room)
            .WithMessage("Выберите допустимое поле сортировки.")
            .OverridePropertyName("sortBy");

        RuleFor(query => query.SortDirection)
            .Must(value => value is EquipmentSorting.Ascending or EquipmentSorting.Descending)
            .WithMessage("Направление сортировки должно быть asc или desc.")
            .OverridePropertyName("sortDirection");
    }
}
