namespace Equipment.Application.EquipmentItems.GetEquipmentById;

public sealed record GetEquipmentByIdQuery(
    int Id) : IQuery<EquipmentDto>;
