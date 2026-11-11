namespace Equipment.Web.Health;

internal sealed class DatabaseReadinessCheck : IHealthCheck
{
    private readonly IEquipmentReadStore _store;

    public DatabaseReadinessCheck(
        IEquipmentReadStore store)
    {
        _store = store;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await _store.GetTypesAsync(cancellationToken);
        return HealthCheckResult.Healthy();
    }
}
