namespace Equipment.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Регистрирует PostgreSQL и реализации портов в пределах scope
    /// </summary>
    public static IServiceCollection AddEquipmentInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        services.AddDbContext<EquipmentDbContext>(options =>
            options.UseNpgsql(connectionString, postgres => postgres.SetPostgresVersion(18, 0)
            .CommandTimeout(10)));
        services.AddScoped<IEquipmentReadStore, EfEquipmentReadStore>();
        services.AddScoped<IEquipmentWriteStore, EfEquipmentWriteStore>();
        services.AddScoped<DatabaseBootstrapper>();
        return services;
    }
}
