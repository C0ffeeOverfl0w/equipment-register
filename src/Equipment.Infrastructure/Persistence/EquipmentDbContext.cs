namespace Equipment.Infrastructure.Persistence;

public sealed class EquipmentDbContext : DbContext
{
    public EquipmentDbContext(
        DbContextOptions<EquipmentDbContext> options) : base(options)
    {
    }

    public DbSet<EquipmentItem> Equipment => Set<EquipmentItem>();
    public DbSet<EquipmentType> EquipmentTypes => Set<EquipmentType>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EquipmentDbContext).Assembly);
    }
}
