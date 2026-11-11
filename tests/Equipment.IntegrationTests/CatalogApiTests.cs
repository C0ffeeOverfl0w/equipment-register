namespace Equipment.IntegrationTests;

[Collection("postgres")]
public sealed class CatalogApiTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public CatalogApiTests(
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
    public async Task EmptyDatabaseHasFourSeededTypesAndNoEquipment()
    {
        EquipmentTypeDto[]? types = await _fixture.Client.GetFromJsonAsync<EquipmentTypeDto[]>("/api/equipment-types");
        PagedResult<EquipmentDto>? page = await _fixture.Client.GetFromJsonAsync<PagedResult<EquipmentDto>>("/api/equipment");
        Assert.Equal(4, types!.Length);
        Assert.Empty(page!.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task PageIsSortedBeforeSlicingAndReportsTotalCount()
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        int typeId = await context.EquipmentTypes.Select(type => type.Id).FirstAsync();
        context.Equipment.AddRange(Enumerable.Range(0, 45).Reverse().Select(index => EquipmentItem.Create($"PC{index:D3}", $"Device {index:D3}", typeId, 100 - index).Value));
        await context.SaveChangesAsync();

        PagedResult<EquipmentDto>? page = await _fixture.Client.GetFromJsonAsync<PagedResult<EquipmentDto>>("/api/equipment?skip=20&take=20");
        Assert.Equal(45, page!.TotalCount);
        Assert.Equal(Enumerable.Range(20, 20).Select(index => $"PC{index:D3}"), page.Items.Select(item => item.InventoryNumber));
        PagedResult<EquipmentDto>? beyondEnd = await _fixture.Client.GetFromJsonAsync<PagedResult<EquipmentDto>>("/api/equipment?skip=100&take=20");
        Assert.Empty(beyondEnd!.Items);
        Assert.Equal(45, beyondEnd.TotalCount);
    }

    [Theory]
    [InlineData("inventoryNumber", "asc", "ABC")]
    [InlineData("inventoryNumber", "desc", "CBA")]
    [InlineData("name", "asc", "BCA")]
    [InlineData("name", "desc", "ACB")]
    [InlineData("room", "asc", "ABC")]
    [InlineData("room", "desc", "CBA")]
    [InlineData("equipmentTypeName", "asc", "ABC")]
    [InlineData("equipmentTypeName", "desc", "CAB")]
    public async Task EveryColumnSortsByItsDisplayedValue(
        string field,
        string direction,
        string expected)
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        await context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE equipment, equipment_types RESTART IDENTITY");
        EquipmentType zeta = EquipmentType.Create("Zeta").Value;
        EquipmentType alpha = EquipmentType.Create("Alpha").Value;
        context.EquipmentTypes.AddRange(zeta, alpha);
        await context.SaveChangesAsync();
        context.Equipment.AddRange(
            EquipmentItem.Create("C", "Bravo", zeta.Id, 100).Value,
            EquipmentItem.Create("A", "Zulu", alpha.Id, 2).Value,
            EquipmentItem.Create("B", "Alpha", alpha.Id, 10).Value);
        await context.SaveChangesAsync();

        PagedResult<EquipmentDto>? page = await _fixture.Client.GetFromJsonAsync<PagedResult<EquipmentDto>>($"/api/equipment?sortBy={field}&sortDirection={direction}");
        Assert.Equal(expected, string.Concat(page!.Items.Select(item => item.InventoryNumber)));
    }

    [Theory]
    [InlineData("skip=-1", "skip")]
    [InlineData("skip=abc", "skip")]
    [InlineData("take=0", "take")]
    [InlineData("take=101", "take")]
    [InlineData("take=1.5", "take")]
    [InlineData("sortBy=notAColumn", "sortBy")]
    [InlineData("sortDirection=up", "sortDirection")]
    public async Task InvalidDirectHttpQueriesReturnValidationProblem(
        string query,
        string expectedField)
    {
        HttpResponseMessage response = await _fixture.Client.GetAsync($"/api/equipment?{query}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using JsonDocument body = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("validation_error", body.RootElement.GetProperty("code").GetString());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(expectedField, out _));
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
    }

    [Fact]
    public async Task CardIsProjectedWithTypeNameAndMissingCardReturns404()
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        EquipmentType type = await context.EquipmentTypes.FirstAsync();
        EquipmentItem item = EquipmentItem.Create("000PC", "<script>Пример 🖥</script>", type.Id, 1).Value;
        context.Equipment.Add(item);
        await context.SaveChangesAsync();
        EquipmentDto? card = await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{item.Id}");
        Assert.Equal(item.Name, card!.Name);
        Assert.Equal(type.Name, card.EquipmentTypeName);
        Assert.Equal(1, card.Version);
        HttpResponseMessage missing = await _fixture.Client.GetAsync("/api/equipment/2147483647");
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        using JsonDocument missingBody = await System.Text.Json.JsonDocument.ParseAsync(await missing.Content.ReadAsStreamAsync());
        Assert.Equal("equipment_not_found", missingBody.RootElement.GetProperty("code").GetString());
    }

    [Theory]
    [InlineData("ПК1", "Device", 1, "23514")]
    [InlineData("PC1", "", 1, "23514")]
    [InlineData("PC1", "Device", 0, "23514")]
    [InlineData("PC1", "Device", 1001, "23514")]
    public async Task DatabaseConstraintsProtectWritesOutsideTheDomain(
        string number,
        string name,
        int room,
        string sqlState)
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        int typeId = await context.EquipmentTypes.Select(type => type.Id).FirstAsync();
        PostgresException error = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO equipment(inventory_number, name, equipment_type_id, room) VALUES ({number}, {name}, {typeId}, {room})"));
        Assert.Equal(sqlState, error.SqlState);
    }

    [Fact]
    public async Task DatabaseEnforcesUniqueNumberAndForeignKey()
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        int typeId = await context.EquipmentTypes.Select(type => type.Id).FirstAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment(inventory_number, name, equipment_type_id, room) VALUES ({"PC1"}, {"Device"}, {typeId}, {1})");
        PostgresException duplicate = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO equipment(inventory_number, name, equipment_type_id, room) VALUES ({"PC1"}, {"Other"}, {typeId}, {2})"));
        Assert.Equal(PostgresErrorCodes.UniqueViolation, duplicate.SqlState);
        PostgresException foreignKey = await Assert.ThrowsAsync<PostgresException>(() => context.Database.ExecuteSqlInterpolatedAsync(
            $"INSERT INTO equipment(inventory_number, name, equipment_type_id, room) VALUES ({"PC2"}, {"Device"}, {int.MaxValue}, {1})"));
        Assert.Equal(PostgresErrorCodes.ForeignKeyViolation, foreignKey.SqlState);
    }

    [Fact]
    public async Task HealthEndpointsAreAvailableAfterInitialization()
    {
        Assert.Equal(HttpStatusCode.OK, (await _fixture.Client.GetAsync("/health/live")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _fixture.Client.GetAsync("/health/ready")).StatusCode);
    }
}
