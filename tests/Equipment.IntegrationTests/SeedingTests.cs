namespace Equipment.IntegrationTests;

[Collection("postgres")]
public sealed class SeedingTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public SeedingTests(
        PostgreSqlFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync()
    {
        return _fixture.ResetAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task ConcurrentInitializationsSeedEmptyDictionaryExactlyOnce()
    {
        await using (EquipmentDbContext context = _fixture.CreateContext())
        {
            await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE equipment, equipment_types RESTART IDENTITY");
        }

        await Task.WhenAll(Enumerable.Range(0, 4).Select(async _ =>
        {
            await using EquipmentDbContext context = _fixture.CreateContext();
            await new DatabaseBootstrapper(context).SeedTypesAsync(CancellationToken.None);
        }));

        await using EquipmentDbContext verification = _fixture.CreateContext();
        string[] names = await verification.EquipmentTypes.Select(type => type.Name).ToArrayAsync();
        Assert.Equal(4, names.Length);
        Assert.Equal(4, names.Distinct().Count());
    }

    [Fact]
    public async Task ExistingSingleTypeIsPreservedWithoutToppingUp()
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE equipment, equipment_types RESTART IDENTITY");
        context.EquipmentTypes.Add(EquipmentType.Create("Пользовательский тип").Value);
        await context.SaveChangesAsync();

        await new DatabaseBootstrapper(context).SeedTypesAsync(CancellationToken.None);
        await new DatabaseBootstrapper(context).SeedTypesAsync(CancellationToken.None);
        Assert.Equal("Пользовательский тип", (await context.EquipmentTypes.SingleAsync()).Name);
    }
}
