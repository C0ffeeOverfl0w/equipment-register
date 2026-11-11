namespace Equipment.Infrastructure.Catalog;

internal sealed class EfEquipmentReadStore : IEquipmentReadStore
{
    private readonly EquipmentDbContext _context;

    public EfEquipmentReadStore(
        EquipmentDbContext context)
    {
        _context = context;
    }

    private static readonly Expression<Func<EquipmentItem, EquipmentDto>> Projection = item => new EquipmentDto(
        item.Id, item.InventoryNumber, item.Name, item.EquipmentTypeId, item.Type.Name, item.Room, item.Version);

    public async Task<PagedResult<EquipmentDto>> ListAsync(
        EquipmentListQuery query,
        CancellationToken cancellationToken)
    {
        IQueryable<EquipmentItem> source = _context.Equipment
            .AsNoTracking();
        int totalCount = await source.CountAsync(cancellationToken);
        IOrderedQueryable<EquipmentItem> ordered = (query.SortBy, query.Descending) switch
        {
            (EquipmentSortField.InventoryNumber, false) => source.OrderBy(item => item.InventoryNumber),
            (EquipmentSortField.InventoryNumber, true) => source.OrderByDescending(item => item.InventoryNumber),
            (EquipmentSortField.EquipmentTypeName, false) => source.OrderBy(item => item.Type.Name),
            (EquipmentSortField.EquipmentTypeName, true) => source.OrderByDescending(item => item.Type.Name),
            (EquipmentSortField.Name, false) => source.OrderBy(item => item.Name),
            (EquipmentSortField.Name, true) => source.OrderByDescending(item => item.Name),
            (EquipmentSortField.Room, false) => source.OrderBy(item => item.Room),
            (EquipmentSortField.Room, true) => source.OrderByDescending(item => item.Room),
            _ => throw new InvalidOperationException("Unsupported equipment sort field.")
        };

        List<EquipmentDto> items = await ordered
            .ThenBy(item => item.Id)
            .Skip(query.Skip)
            .Take(query.Take)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<EquipmentDto>(items, totalCount);
    }

    public Task<EquipmentDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return _context.Equipment
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EquipmentTypeDto>> GetTypesAsync(
        CancellationToken cancellationToken)
    {
        return await _context.EquipmentTypes
            .AsNoTracking()
            .OrderBy(type => type.Name)
            .ThenBy(type => type.Id)
            .Select(type => new EquipmentTypeDto(type.Id, type.Name))
            .ToListAsync(cancellationToken);
    }
}
