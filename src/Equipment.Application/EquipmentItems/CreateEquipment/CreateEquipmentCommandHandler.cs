namespace Equipment.Application.EquipmentItems.CreateEquipment;

/// <inheritdoc />
internal sealed class CreateEquipmentCommandHandler : ICommandHandler<CreateEquipmentCommand, EquipmentDto>
{
    private readonly IEquipmentWriteStore _store;

    public CreateEquipmentCommandHandler(
        IEquipmentWriteStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentDto>> Handle(
        CreateEquipmentCommand command,
        CancellationToken cancellationToken)
    {
        Result<EquipmentItem> result = EquipmentItem.Create(
            command.Details.InventoryNumber, command.Details.Name, command.Details.EquipmentTypeId, command.Details.Room);

        if (result.IsFailure)
        {
            return Result.Failure<EquipmentDto>(result.Error);
        }

        return await _store.CreateAsync(result.Value, EquipmentRules.MaximumEquipmentCount, cancellationToken);
    }
}
