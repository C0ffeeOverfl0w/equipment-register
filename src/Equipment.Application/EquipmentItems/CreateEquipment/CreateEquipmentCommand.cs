namespace Equipment.Application.EquipmentItems.CreateEquipment;

public sealed record CreateEquipmentCommand(
    EquipmentDetails Details) : ICommand<EquipmentDto>;
