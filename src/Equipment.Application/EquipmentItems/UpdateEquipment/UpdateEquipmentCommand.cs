namespace Equipment.Application.EquipmentItems.UpdateEquipment;

public sealed record UpdateEquipmentCommand(
    int Id,
    EquipmentDetails Details,
    long ExpectedVersion) : ICommand<EquipmentDto>;
