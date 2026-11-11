namespace Equipment.IntegrationTests;

[Collection("postgres")]
public sealed class WriteStoreScopeTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public WriteStoreScopeTests(
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
    public async Task FailedCreateDoesNotPoisonNextCommandInSameScope()
    {
        await using AsyncServiceScope scope = _fixture.CreateScope();
        IEquipmentWriteStore store = scope.ServiceProvider.GetRequiredService<IEquipmentWriteStore>();
        Assert.True((await store.CreateAsync(Item("PC1"), 2000, CancellationToken.None)).IsSuccess);
        Assert.Equal(EquipmentErrors.InventoryNumberConflict,
            (await store.CreateAsync(Item("PC1"), 2000, CancellationToken.None)).Error);
        Assert.True((await store.CreateAsync(Item("PC2"), 2000, CancellationToken.None)).IsSuccess);

        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(2, await context.Equipment.CountAsync());
    }

    [Fact]
    public async Task FailedUpdateIsNotAccidentallySavedByNextCommand()
    {
        await using AsyncServiceScope scope = _fixture.CreateScope();
        IEquipmentWriteStore store = scope.ServiceProvider.GetRequiredService<IEquipmentWriteStore>();
        EquipmentDto created = (await store.CreateAsync(Item("PC1"), 2000, CancellationToken.None)).Value;
        EquipmentItem item = (await store.FindForUpdateAsync(created.Id, CancellationToken.None))!;
        item.UpdateDetails("PC1", "Invalid change", 999, 2);
        Assert.Equal(EquipmentErrors.UnknownType,
            (await store.SaveUpdatedAsync(item, 1, CancellationToken.None)).Error);
        Assert.True((await store.CreateAsync(Item("PC2"), 2000, CancellationToken.None)).IsSuccess);

        await using EquipmentDbContext context = _fixture.CreateContext();
        EquipmentItem persisted = await context.Equipment.SingleAsync(entity => entity.Id == created.Id);
        Assert.Equal("Original", persisted.Name);
        Assert.Equal(1, persisted.Version);
    }

    [Fact]
    public async Task DatabaseVersionGuardRejectsRaceAfterBothEntitiesWereLoaded()
    {
        await using AsyncServiceScope firstScope = _fixture.CreateScope();
        await using AsyncServiceScope secondScope = _fixture.CreateScope();
        IEquipmentWriteStore firstStore = firstScope.ServiceProvider.GetRequiredService<IEquipmentWriteStore>();
        IEquipmentWriteStore secondStore = secondScope.ServiceProvider.GetRequiredService<IEquipmentWriteStore>();
        int id = (await firstStore.CreateAsync(Item("PC1"), 2000, CancellationToken.None)).Value.Id;
        EquipmentItem first = (await firstStore.FindForUpdateAsync(id, CancellationToken.None))!;
        EquipmentItem second = (await secondStore.FindForUpdateAsync(id, CancellationToken.None))!;
        first.UpdateDetails("PC1", "Winner", 1, 2);
        second.UpdateDetails("PC1", "Loser", 1, 3);

        Assert.True((await firstStore.SaveUpdatedAsync(first, 1, CancellationToken.None)).IsSuccess);
        Assert.Equal(EquipmentErrors.VersionConflict,
            (await secondStore.SaveUpdatedAsync(second, 1, CancellationToken.None)).Error);

        await using EquipmentDbContext context = _fixture.CreateContext();
        EquipmentItem persisted = await context.Equipment.SingleAsync(entity => entity.Id == id);
        Assert.Equal("Winner", persisted.Name);
        Assert.Equal(2, persisted.Version);
    }

    private static EquipmentItem Item(
        string number)
    {
        return EquipmentItem.Create(number, "Original", 1, 1).Value;
    }
}
