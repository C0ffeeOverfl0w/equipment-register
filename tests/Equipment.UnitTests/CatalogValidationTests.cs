namespace Equipment.UnitTests;

public sealed class CatalogValidationTests
{
    [Theory]
    [InlineData(-1, 20, "room", "asc", "skip")]
    [InlineData(0, 0, "room", "asc", "take")]
    [InlineData(0, 101, "room", "asc", "take")]
    [InlineData(0, 20, "room; DROP TABLE equipment", "asc", "sortBy")]
    [InlineData(0, 20, "0", "asc", "sortBy")]
    [InlineData(0, 20, "room", "up", "sortDirection")]
    public async Task InvalidQueryNeverReachesDataStore(
        int skip,
        int take,
        string field,
        string direction,
        string errorField)
    {
        var store = new RecordingStore();
        var query = new GetEquipmentQuery(skip, take, field, direction);
        var behavior = new ValidationBehavior<GetEquipmentQuery, Result<PagedResult<EquipmentDto>>>([new GetEquipmentQueryValidator()]);
        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(
            query,
            cancellationToken => new GetEquipmentQueryHandler(store).Handle(query, cancellationToken),
            CancellationToken.None));
        Assert.Contains(errorField, exception.Errors.Keys);
        Assert.False(store.Called);
    }

    [Fact]
    public async Task AllowedQueryPassesTypedSortingAndCancellationToStore()
    {
        var store = new RecordingStore();
        using var cancellation = new CancellationTokenSource();
        await new GetEquipmentQueryHandler(store)
            .Handle(new GetEquipmentQuery(20, 50, "equipmentTypeName", "desc"), cancellation.Token);
        Assert.Equal(new EquipmentListQuery(20, 50, EquipmentSortField.EquipmentTypeName, true), store.Query);
        Assert.Equal(cancellation.Token, store.CancellationToken);
    }

    private sealed class RecordingStore : IEquipmentReadStore
    {
        public bool Called { get; private set; }
        public EquipmentListQuery? Query { get; private set; }
        public CancellationToken CancellationToken { get; private set; }

        public Task<PagedResult<EquipmentDto>> ListAsync(
            EquipmentListQuery query,
            CancellationToken cancellationToken)
        {
            Called = true;
            Query = query;
            CancellationToken = cancellationToken;
            return Task.FromResult(new PagedResult<EquipmentDto>([], 0));
        }

        public Task<EquipmentDto?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<IReadOnlyList<EquipmentTypeDto>> GetTypesAsync(
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
