namespace Equipment.UnitTests;

public sealed class EquipmentCommandsTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task InvalidVersionNeverLoadsOrSaves(
        long version)
    {
        var store = new FakeStore();
        var behavior = new ValidationBehavior<UpdateEquipmentCommand, Result<EquipmentDto>>([new UpdateEquipmentCommandValidator()]);
        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(
            Update(version),
            cancellationToken => new UpdateEquipmentCommandHandler(store).Handle(Update(version), cancellationToken),
            CancellationToken.None));
        Assert.Contains("version", exception.Errors.Keys);
        Assert.False(store.Loaded);
        Assert.False(store.Saved);
    }

    [Fact]
    public async Task StaleVersionDoesNotMutateLoadedEntity()
    {
        var store = new FakeStore { Item = EquipmentItem.Create("PC1", "Исходная", 1, 1).Value };
        store.Item.UpdateDetails("PC1", "Чужое изменение", 1, 2);
        Result<EquipmentDto> result = await new UpdateEquipmentCommandHandler(store)
            .Handle(Update(1), CancellationToken.None);
        Assert.Equal(EquipmentErrors.VersionConflict, result.Error);
        Assert.Equal("Чужое изменение", store.Item.Name);
        Assert.False(store.Saved);
    }

    [Fact]
    public async Task MissingEntityReturnsNotFound()
    {
        Result<EquipmentDto> result = await new UpdateEquipmentCommandHandler(new FakeStore())
            .Handle(Update(1), CancellationToken.None);
        Assert.Equal(EquipmentErrors.NotFound, result.Error);
    }

    [Fact]
    public async Task InvalidCreateDoesNotReachStore()
    {
        var store = new FakeStore();
        Result<EquipmentDto> result = await new CreateEquipmentCommandHandler(store)
            .Handle(new CreateEquipmentCommand(new EquipmentDetails("ПК", "Техника", 1, 1)), CancellationToken.None);
        Assert.Equal(EquipmentErrors.InvalidInventoryNumber, result.Error);
        Assert.Null(store.Item);
    }

    [Fact]
    public async Task InvalidUpdateDoesNotSaveOrPartiallyMutateEntity()
    {
        var store = new FakeStore { Item = EquipmentItem.Create("PC1", "Исходная", 1, 1).Value };
        Result<EquipmentDto> result = await new UpdateEquipmentCommandHandler(store)
            .Handle(new UpdateEquipmentCommand(1, new EquipmentDetails("PC2", " ", 2, 3), 1), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal("PC1", store.Item.InventoryNumber);
        Assert.Equal(1, store.Item.Version);
        Assert.False(store.Saved);
    }

    [Fact]
    public async Task CreatePassesBusinessCapacityAndCancellationToAtomicStore()
    {
        var store = new FakeStore();
        using var cancellation = new CancellationTokenSource();
        Result<EquipmentDto> result = await new CreateEquipmentCommandHandler(store).Handle(
            new CreateEquipmentCommand(new EquipmentDetails("000PC", "Техника", 1, 1000)), cancellation.Token);
        Assert.True(result.IsSuccess);
        Assert.Equal(2000, store.Capacity);
        Assert.Equal("000PC", store.Item!.InventoryNumber);
        Assert.Equal(cancellation.Token, store.CancellationToken);
    }

    [Fact]
    public async Task ConcurrentStoreFailureIsPreservedByHandler()
    {
        var store = new FakeStore
        {
            Item = EquipmentItem.Create("PC1", "Исходная", 1, 1).Value,
            SaveError = EquipmentErrors.VersionConflict
        };
        Result<EquipmentDto> result = await new UpdateEquipmentCommandHandler(store).Handle(Update(1), CancellationToken.None);
        Assert.Equal(EquipmentErrors.VersionConflict, result.Error);
        Assert.True(store.Saved);
        Assert.Equal(1, store.ExpectedVersion);
    }

    [Fact]
    public async Task UnexpectedStorageFailureIsNotDisguisedAsBusinessError()
    {
        var store = new FakeStore { ThrowOnCreate = true };
        await Assert.ThrowsAsync<IOException>(() => new CreateEquipmentCommandHandler(store).Handle(
            new CreateEquipmentCommand(new EquipmentDetails("PC1", "Техника", 1, 1)), CancellationToken.None));
    }

    private static UpdateEquipmentCommand Update(
        long version)
    {
        return new(1, new EquipmentDetails("PC1", "Новая", 1, 2), version);
    }

    private sealed class FakeStore : IEquipmentWriteStore
    {
        public EquipmentItem? Item { get; set; }
        public Error? SaveError { get; init; }
        public bool ThrowOnCreate { get; init; }
        public bool Loaded { get; private set; }
        public bool Saved { get; private set; }
        public int Capacity { get; private set; }
        public long ExpectedVersion { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<Result<EquipmentDto>> CreateAsync(
            EquipmentItem item,
            int maximumCount,
            CancellationToken cancellationToken)
        {
            if (ThrowOnCreate)
            {
                throw new IOException("Storage unavailable");
            }

            Item = item;
            Capacity = maximumCount;
            CancellationToken = cancellationToken;
            return Task.FromResult(Result.Success(ToDto(item)));
        }

        public Task<EquipmentItem?> FindForUpdateAsync(
            int id,
            CancellationToken cancellationToken)
        {
            Loaded = true;
            return Task.FromResult(Item);
        }

        public Task<Result<EquipmentDto>> SaveUpdatedAsync(
            EquipmentItem item,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            Saved = true;
            ExpectedVersion = expectedVersion;
            return Task.FromResult(SaveError is null ? Result.Success(ToDto(item)) : Result.Failure<EquipmentDto>(SaveError));
        }

        private static EquipmentDto ToDto(
            EquipmentItem item)
        {
            return new(item.Id, item.InventoryNumber, item.Name, item.EquipmentTypeId, "Тип", item.Room, item.Version);
        }
    }
}
