namespace Equipment.IntegrationTests;

[Collection("postgres")]
public sealed class EquipmentWriteApiTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public EquipmentWriteApiTests(
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

    [Theory]
    [InlineData(1)]
    [InlineData(1000)]
    public async Task CreateReturns201LocationAndPersistedCard(
        int room)
    {
        HttpResponseMessage response = await _fixture.Client.PostAsJsonAsync("/api/equipment", Details(room: room));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        EquipmentDto card = (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
        Assert.True(card.Id > 0);
        Assert.Equal("000PC", card.InventoryNumber);
        Assert.Equal(room, card.Room);
        Assert.Equal(1, card.Version);
        Assert.EndsWith($"/api/equipment/{card.Id}", response.Headers.Location!.ToString());
        Assert.Equal(card, await _fixture.Client.GetFromJsonAsync<EquipmentDto>(response.Headers.Location));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{invalid")]
    [InlineData("{\"inventoryNumber\":null,\"name\":\"PC\",\"equipmentTypeId\":1,\"room\":1}")]
    [InlineData("{\"inventoryNumber\":\"PC1\",\"name\":\"PC\",\"equipmentTypeId\":1,\"room\":1.5}")]
    [InlineData("{\"inventoryNumber\":\"PC1\",\"name\":\"PC\",\"equipmentTypeId\":1}")]
    [InlineData("{\"inventoryNumber\":\"PC1\",\"name\":\"PC\",\"room\":1}")]
    [InlineData("{\"inventoryNumber\":\"PC1\",\"name\":\"PC\",\"equipmentTypeId\":1,\"room\":1,\"id\":999}")]
    [InlineData("{\"inventoryNumber\":\"PC1\",\"name\":\"PC\",\"equipmentTypeId\":1,\"room\":1,\"version\":99}")]
    public async Task InvalidJsonShapeCannotCreateAnything(
        string json)
    {
        HttpResponseMessage response = await _fixture.Client.PostAsync("/api/equipment", new StringContent(json, Encoding.UTF8, "application/json"));
        await AssertProblem(response, HttpStatusCode.BadRequest, "validation_error");
        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(0, await context.Equipment.CountAsync());
    }

    [Theory]
    [InlineData("inventoryNumber", "ПК1")]
    [InlineData("inventoryNumber", "PC-1")]
    [InlineData("inventoryNumber", "")]
    [InlineData("name", " \t\n")]
    public async Task InvalidDomainTextIsRejectedByDirectHttp(
        string field,
        string value)
    {
        JsonObject body = JsonSerializer.SerializeToNode(Details())!.AsObject();
        body[field] = value;
        HttpResponseMessage response = await _fixture.Client.PostAsJsonAsync("/api/equipment", body);
        await AssertProblem(response, HttpStatusCode.BadRequest, "validation_error");
    }

    [Theory]
    [InlineData("inventoryNumber", 33)]
    [InlineData("name", 257)]
    public async Task LengthOverflowIsRejected(
        string field,
        int length)
    {
        JsonObject body = JsonSerializer.SerializeToNode(Details())!.AsObject();
        body[field] = new string('A', length);
        await AssertProblem(await _fixture.Client.PostAsJsonAsync("/api/equipment", body), HttpStatusCode.BadRequest, "validation_error");
    }

    [Fact]
    public async Task UnicodeNameBoundaryAndMaximumNumberAreAccepted()
    {
        HttpResponseMessage response = await _fixture.Client.PostAsJsonAsync("/api/equipment", Details(number: new string('A', 32), name: string.Concat(Enumerable.Repeat("🖥", 256))));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1001)]
    public async Task RoomOutOfRangeIsRejected(
        int room)
    {
        await AssertProblem(await _fixture.Client.PostAsJsonAsync("/api/equipment", Details(room: room)), HttpStatusCode.BadRequest, "validation_error");
    }

    [Fact]
    public async Task UnknownTypeIsAFieldErrorAndDoesNotPersist()
    {
        HttpResponseMessage response = await _fixture.Client.PostAsJsonAsync("/api/equipment", Details(typeId: int.MaxValue));
        JsonElement problem = await AssertProblem(response, HttpStatusCode.BadRequest, "validation_error");
        Assert.True(problem.GetProperty("errors").TryGetProperty("equipmentTypeId", out _));
        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Empty(await context.Equipment.ToListAsync());
    }

    [Fact]
    public async Task RepeatedAndConcurrentCreateCannotDuplicateNumber()
    {
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 4).Select(_ => _fixture.Client.PostAsJsonAsync("/api/equipment", Details())));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        foreach (HttpResponseMessage? conflict in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
        {
            await AssertProblem(conflict, HttpStatusCode.Conflict, "inventory_number_conflict");
        }

        await AssertProblem(await _fixture.Client.PostAsJsonAsync("/api/equipment", Details()), HttpStatusCode.Conflict, "inventory_number_conflict");
        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(1, await context.Equipment.CountAsync());
    }

    [Fact]
    public async Task UpdateReplacesAllFieldsAndKeepsItsOwnNumber()
    {
        EquipmentDto card = await CreateCard();
        HttpResponseMessage response = await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(card.Version, name: "Новая карточка", typeId: 2, room: 1000));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        EquipmentDto updated = (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
        Assert.Equal(card.Id, updated.Id);
        Assert.Equal(card.InventoryNumber, updated.InventoryNumber);
        Assert.Equal("Новая карточка", updated.Name);
        Assert.Equal(2, updated.EquipmentTypeId);
        Assert.Equal(1000, updated.Room);
        Assert.Equal(2, updated.Version);
        Assert.NotEqual(card.EquipmentTypeName, updated.EquipmentTypeName);
        Assert.Equal(updated, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{card.Id}"));
    }

    [Fact]
    public async Task ConflictingNumberRollsBackOtherChangedFieldsAndVersion()
    {
        EquipmentDto first = await CreateCard();
        await CreateCard("OTHER");
        HttpResponseMessage response = await _fixture.Client.PutAsJsonAsync($"/api/equipment/{first.Id}", Update(first.Version, number: "OTHER", name: "Не сохранять", room: 999));
        await AssertProblem(response, HttpStatusCode.Conflict, "inventory_number_conflict");
        Assert.Equal(first, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{first.Id}"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task NonPositiveVersionIsRejected(
        long version)
    {
        EquipmentDto card = await CreateCard();
        await AssertProblem(await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(version)), HttpStatusCode.BadRequest, "validation_error");
        Assert.Equal(card, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{card.Id}"));
    }

    [Fact]
    public async Task MissingVersionOrPartialPutCannotEraseFields()
    {
        EquipmentDto card = await CreateCard();
        await AssertProblem(await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Details()), HttpStatusCode.BadRequest, "validation_error");
        await AssertProblem(await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", new
        {
            room = 2,
            version = card.Version
        }), HttpStatusCode.BadRequest, "validation_error");
        Assert.Equal(card, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{card.Id}"));
    }

    [Fact]
    public async Task UnknownTypeDuringUpdateDoesNotChangeVersionOrFields()
    {
        EquipmentDto card = await CreateCard();
        await AssertProblem(await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(1, typeId: int.MaxValue, room: 2)), HttpStatusCode.BadRequest, "validation_error");
        Assert.Equal(card, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{card.Id}"));
    }

    [Fact]
    public async Task MissingCardUpdateReturns404()
    {
        await AssertProblem(await _fixture.Client.PutAsJsonAsync("/api/equipment/2147483647", Update(1)), HttpStatusCode.NotFound, "equipment_not_found");
    }

    [Fact]
    public async Task OnlyOneConcurrentUpdateOfTheSameVersionSucceeds()
    {
        EquipmentDto card = await CreateCard();
        HttpResponseMessage[] responses = await Task.WhenAll(
            _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(card.Version, name: "Первый", room: 2)),
            _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(card.Version, name: "Второй", room: 3)));
        HttpResponseMessage success = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        HttpResponseMessage conflict = Assert.Single(responses, response => response.StatusCode != HttpStatusCode.OK);
        await AssertProblem(conflict, HttpStatusCode.Conflict, "equipment_version_conflict");
        EquipmentDto? winner = await success.Content.ReadFromJsonAsync<EquipmentDto>();
        Assert.Equal(2, winner!.Version);
        Assert.Equal(winner, await _fixture.Client.GetFromJsonAsync<EquipmentDto>($"/api/equipment/{card.Id}"));
        await AssertProblem(await _fixture.Client.PutAsJsonAsync($"/api/equipment/{card.Id}", Update(card.Version)), HttpStatusCode.Conflict, "equipment_version_conflict");
    }

    [Fact]
    public async Task ConcurrentCreatesCanOccupyOnlyOneRemainingSlot()
    {
        await FillDatabase(1999);
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 12).Select(index =>
            _fixture.Client.PostAsJsonAsync("/api/equipment", Details(number: $"LAST{index}"))));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        foreach (HttpResponseMessage? conflict in responses.Where(response => response.StatusCode != HttpStatusCode.Created))
        {
            await AssertProblem(conflict, HttpStatusCode.Conflict, "equipment_limit_reached");
        }

        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(2000, await context.Equipment.CountAsync());
    }

    [Fact]
    public async Task FullDatabaseStillAllowsEditingExistingCard()
    {
        await FillDatabase(2000);
        await AssertProblem(await _fixture.Client.PostAsJsonAsync("/api/equipment", Details()), HttpStatusCode.Conflict, "equipment_limit_reached");
        await using EquipmentDbContext context = _fixture.CreateContext();
        int id = await context.Equipment.OrderBy(item => item.Id)
            .Select(item => item.Id).FirstAsync();
        HttpResponseMessage response = await _fixture.Client.PutAsJsonAsync($"/api/equipment/{id}", Update(1, number: "UPDATED", room: 1000));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2000, await context.Equipment.CountAsync());
    }

    [Fact]
    public async Task FailedDuplicateReleasesLockAndDoesNotConsumeCapacity()
    {
        await FillDatabase(1998);
        await CreateCard();
        await AssertProblem(await _fixture.Client.PostAsJsonAsync("/api/equipment", Details()), HttpStatusCode.Conflict, "inventory_number_conflict");
        await CreateCard("LAST");
        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(2000, await context.Equipment.CountAsync());
    }

    private async Task<EquipmentDto> CreateCard(
        string number = "000PC")
    {
        HttpResponseMessage response = await _fixture.Client.PostAsJsonAsync("/api/equipment", Details(number: number));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<EquipmentDto>())!;
    }

    private async Task FillDatabase(
        int count)
    {
        await using EquipmentDbContext context = _fixture.CreateContext();
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment(inventory_number,name,equipment_type_id,room) SELECT 'FILL' || series::text, 'Fixture', 1, 1 FROM generate_series(1,{count}) AS series");
    }

    private static object Details(
        string number = "000PC",
        string name = "Техника",
        int typeId = 1,
        int room = 1)
    {
        return new
        {
            inventoryNumber = number,
            name,
            equipmentTypeId = typeId,
            room
        };
    }

    private static object Update(
        long version,
        string number = "000PC",
        string name = "Техника",
        int typeId = 1,
        int room = 1)
    {
        return new
        {
            inventoryNumber = number,
            name,
            equipmentTypeId = typeId,
            room,
            version
        };
    }

    private static async Task<JsonElement> AssertProblem(
        HttpResponseMessage response,
        HttpStatusCode expected,
        string code)
    {
        string text = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == expected, $"Expected {(int)expected}, received {(int)response.StatusCode}: {text}");
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var document = JsonDocument.Parse(text);
        JsonElement problem = document.RootElement.Clone();
        Assert.Equal(code, problem.GetProperty("code").GetString());
        Assert.True(problem.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("Npgsql", text);
        Assert.False(problem.TryGetProperty("exception", out _));
        return problem;
    }
}
