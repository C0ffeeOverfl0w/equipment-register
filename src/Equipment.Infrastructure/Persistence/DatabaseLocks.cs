namespace Equipment.Infrastructure.Persistence;

/// <summary>
/// Определяет транзакционные блокировки модуля, согласованные со скриптом демонстрационных данных
/// </summary>
internal static class DatabaseLocks
{
    public const string SeedEquipmentTypes = "SELECT pg_advisory_xact_lock(733101, 1)";
    public const string CreateEquipment = "SELECT pg_advisory_xact_lock(733101, 2)";
}
