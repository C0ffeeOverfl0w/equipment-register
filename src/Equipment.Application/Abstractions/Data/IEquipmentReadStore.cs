namespace Equipment.Application.Abstractions.Data;

public interface IEquipmentReadStore
{
    Task<PagedResult<EquipmentDto>> ListAsync(
        EquipmentListQuery query,
        CancellationToken cancellationToken);
    Task<EquipmentDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);
    Task<IReadOnlyList<EquipmentTypeDto>> GetTypesAsync(
        CancellationToken cancellationToken);
}
