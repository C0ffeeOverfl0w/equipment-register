namespace Equipment.Infrastructure.Persistence;

public sealed class DatabaseBootstrapper
{
    private readonly EquipmentDbContext _context;

    public DatabaseBootstrapper(
        EquipmentDbContext context)
    {
        _context = context;
    }

    public Task MigrateAsync(
        CancellationToken cancellationToken)
    {
        return _context.Database.MigrateAsync(cancellationToken);
    }

    public async Task SeedTypesAsync(
        CancellationToken cancellationToken)
    {
        await using IDbContextTransaction transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken);
        await _context.Database.ExecuteSqlRawAsync("SELECT pg_advisory_xact_lock(733101, 1)", cancellationToken);

        if (!await _context.EquipmentTypes.AnyAsync(cancellationToken))
        {
            _context.EquipmentTypes.AddRange(
                EquipmentType.Create("Системный блок").Value,
                EquipmentType.Create("Ноутбук").Value,
                EquipmentType.Create("Монитор").Value,
                EquipmentType.Create("Принтер").Value);
            await _context.SaveChangesAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
}
