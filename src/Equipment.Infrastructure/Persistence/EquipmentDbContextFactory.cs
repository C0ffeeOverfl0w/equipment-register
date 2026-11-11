namespace Equipment.Infrastructure.Persistence;

public sealed class EquipmentDbContextFactory : IDesignTimeDbContextFactory<EquipmentDbContext>
{
    public EquipmentDbContext CreateDbContext(
        string[] args)
    {
        string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__Equipment")
            ?? "Host=127.0.0.1;Port=55432;Database=equipment;Username=equipment;Password=equipment_dev_only";
        DbContextOptions<EquipmentDbContext> options = new DbContextOptionsBuilder<EquipmentDbContext>()
            .UseNpgsql(connectionString, postgres => postgres.SetPostgresVersion(18, 0))
            .Options;
        return new EquipmentDbContext(options);
    }
}
