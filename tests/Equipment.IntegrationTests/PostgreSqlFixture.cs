namespace Equipment.IntegrationTests;

[CollectionDefinition("postgres")]
public sealed class PostgreSqlCollection : ICollectionFixture<PostgreSqlFixture>;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:18.6")
        .WithDatabase("equipment_test")
        .WithUsername("equipment_test")
        .WithPassword("integration_tests_only")
        .Build();
    private WebApplicationFactory<Program> _factory = null!;
    public HttpClient Client { get; private set; } = null!;

    public AsyncServiceScope CreateScope()
    {
        return _factory.Services.CreateAsyncScope();
    }

    public WebApplicationFactory<Program> CreateFactory(
        Action<IWebHostBuilder> configure)
    {
        return _factory.WithWebHostBuilder(configure);
    }

    public EquipmentDbContext CreateContext()
    {
        return new(new DbContextOptionsBuilder<EquipmentDbContext>()
        .UseNpgsql(_database.GetConnectionString(), postgres => postgres.SetPostgresVersion(18, 0)).Options);
    }

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using EquipmentDbContext context = CreateContext();
        await new DatabaseBootstrapper(context).MigrateAsync(CancellationToken.None);
        _factory = new EquipmentApiFactory(_database.GetConnectionString());
        Client = _factory.CreateClient();
    }

    public async Task ResetAsync()
    {
        // Подключение принадлежит временному тестовому контейнеру, а не локальной базе разработчика
        await using EquipmentDbContext context = CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE equipment, equipment_types RESTART IDENTITY");
        await new DatabaseBootstrapper(context).SeedTypesAsync(CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        Client?.Dispose();
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await _database.DisposeAsync();
    }

    private sealed class EquipmentApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _connectionString;

        public EquipmentApiFactory(
            string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void ConfigureWebHost(
            IWebHostBuilder builder)
        {
            builder.UseEnvironment("Production");
            builder.UseSetting("ConnectionStrings:Equipment", _connectionString);
            builder.UseSetting("Logging:LogLevel:Default", "Warning");
        }
    }
}
