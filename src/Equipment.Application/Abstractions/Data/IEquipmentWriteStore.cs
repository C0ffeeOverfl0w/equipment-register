namespace Equipment.Application.Abstractions.Data;

public interface IEquipmentWriteStore
{
    Task<Result<EquipmentDto>> CreateAsync(
        EquipmentItem item,
        int maximumCount,
        CancellationToken cancellationToken);
    Task<EquipmentItem?> FindForUpdateAsync(
        int id,
        CancellationToken cancellationToken);
    Task<Result<EquipmentDto>> SaveUpdatedAsync(
        EquipmentItem item,
        long expectedVersion,
        CancellationToken cancellationToken);
}
