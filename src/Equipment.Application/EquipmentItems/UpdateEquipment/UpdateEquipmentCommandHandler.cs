namespace Equipment.Application.EquipmentItems.UpdateEquipment;

/// <inheritdoc />
internal sealed class UpdateEquipmentCommandHandler : ICommandHandler<UpdateEquipmentCommand, EquipmentDto>
{
    private readonly IEquipmentWriteStore _store;

    public UpdateEquipmentCommandHandler(
        IEquipmentWriteStore store)
    {
        _store = store;
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentDto>> Handle(
        UpdateEquipmentCommand command,
        CancellationToken cancellationToken)
    {
        EquipmentItem? item = await _store.FindForUpdateAsync(command.Id, cancellationToken);
        if (item is null)
        {
            return Result.Failure<EquipmentDto>(EquipmentErrors.NotFound);
        }

        if (item.Version != command.ExpectedVersion)
        {
            return Result.Failure<EquipmentDto>(EquipmentErrors.VersionConflict);
        }

        Result result = item.UpdateDetails(
            command.Details.InventoryNumber, command.Details.Name, command.Details.EquipmentTypeId, command.Details.Room);

        if (result.IsFailure)
        {
            return Result.Failure<EquipmentDto>(result.Error);
        }

        return await _store.SaveUpdatedAsync(item, command.ExpectedVersion, cancellationToken);
    }
}
