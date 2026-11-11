namespace Equipment.IntegrationTests;

[Collection("postgres")]
public sealed class MediatorApiTests : IAsyncLifetime
{
    private readonly PostgreSqlFixture _fixture;

    public MediatorApiTests(
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
    public async Task SenderAppliesValidationWithoutHttpController()
    {
        await using AsyncServiceScope scope = _fixture.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new CreateEquipmentCommand(new EquipmentDetails("ПК", " ", 0, 1001));

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(() => sender.Send(command));

        Assert.Equal(["equipmentTypeId", "inventoryNumber", "name", "room"], exception.Errors.Keys.Order());
        await using EquipmentDbContext context = _fixture.CreateContext();
        Assert.Equal(0, await context.Equipment.CountAsync());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task SenderRejectsInvalidVersionBeforeLoadingMissingCard(
        long version)
    {
        await using AsyncServiceScope scope = _fixture.CreateScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var command = new UpdateEquipmentCommand(999, new EquipmentDetails("PC1", "Device", 1, 1), version);

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(() => sender.Send(command));
        Assert.Contains("version", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("application/json")]
    [InlineData("application/xml")]
    public async Task HttpValidationRetainsFlatFieldsContentTypeAndTraceId(
        string accept)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/equipment");
        request.Headers.Accept.ParseAdd(accept);
        request.Content = JsonContent.Create(new
        {
            inventoryNumber = "ПК",
            name = "Device\0",
            equipmentTypeId = 0,
            room = 1001
        });
        using HttpResponseMessage response = await _fixture.Client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("validation_error", body.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("traceId").GetString()));
        Assert.Equal(["equipmentTypeId", "inventoryNumber", "name", "room"],
            body.RootElement.GetProperty("errors").EnumerateObject().Select(property => property.Name)
            .Order());
    }

    [Fact]
    public async Task UnexpectedFailureReturnsSanitized500ThroughMediator()
    {
        var logs = new CapturingLoggerProvider();
        await using WebApplicationFactory<Program> factory = _fixture.CreateFactory(builder => builder
            .ConfigureLogging(logging => logging.AddProvider(logs).AddFilter<CapturingLoggerProvider>(null, LogLevel.Information))
            .ConfigureTestServices(services =>
        {
            services.RemoveAll<IEquipmentWriteStore>();
            services.AddScoped<IEquipmentWriteStore, BrokenWriteStore>();
        }));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/equipment", new
        {
            inventoryNumber = "PC1",
            name = "Device",
            equipmentTypeId = 1,
            room = 1
        });

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        string content = await response.Content.ReadAsStringAsync();
        using var body = JsonDocument.Parse(content);
        Assert.Equal("unexpected_error", body.RootElement.GetProperty("code").GetString());
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
        Assert.DoesNotContain("internal storage details", content);
        LogEntry error = Assert.Single(logs.Entries, entry => entry.Exception is IOException);
        Assert.Equal(LogLevel.Error, error.Level);
        Assert.Equal(body.RootElement.GetProperty("traceId").GetString(), error.Fields["traceId"]);
    }

    [Fact]
    public async Task ValidationResponseAndPipelineLogsHaveSameTraceId()
    {
        var logs = new CapturingLoggerProvider();
        await using WebApplicationFactory<Program> factory = _fixture.CreateFactory(builder => builder
            .ConfigureLogging(logging => logging.AddProvider(logs).AddFilter<CapturingLoggerProvider>(null, LogLevel.Information)));
        using HttpClient client = factory.CreateClient();
        using HttpResponseMessage response = await client.GetAsync("/api/equipment?skip=-1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        string traceId = body.RootElement.GetProperty("traceId").GetString()!;

        LogEntry[] entries = [.. logs.Entries
            .Where(entry => entry.Fields.TryGetValue("RequestName", out object? name)
            && Equals(name, nameof(GetEquipmentQuery)))];
        Assert.NotEmpty(entries);
        Assert.All(entries, entry => Assert.Equal(traceId, entry.Fields["traceId"]));
        Assert.Contains(entries, entry => entry.Fields.ContainsKey("InvalidFieldCount"));
        Assert.DoesNotContain(logs.Entries, entry => entry.Level >= LogLevel.Error);
    }

    private sealed record LogEntry(
        LogLevel Level,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Fields);

    private sealed class CapturingLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
        public ConcurrentQueue<LogEntry> Entries { get; } = new();

        public ILogger CreateLogger(
            string categoryName)
        {
            return new CapturingLogger(this);
        }

        public void SetScopeProvider(
                    IExternalScopeProvider scopeProvider)
        {
            _scopes = scopeProvider;
        }

        public void Dispose() { }

        private sealed class CapturingLogger : ILogger
        {
            private readonly CapturingLoggerProvider _owner;

            public CapturingLogger(
                CapturingLoggerProvider owner)
            {
                _owner = owner;
            }
            public IDisposable? BeginScope<TState>(
                TState state) where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(
                            LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter)
            {
                Dictionary<string, object?> fields = state is IEnumerable<KeyValuePair<string, object?>> propertiesState
                    ? propertiesState.ToDictionary(pair => pair.Key, pair => pair.Value)
                    : [];
                _owner._scopes.ForEachScope((scope, values) =>
                {
                    if (scope is IEnumerable<KeyValuePair<string, object?>> properties)
                    {
                        foreach (KeyValuePair<string, object?> property in properties)
                        {
                            values[property.Key] = property.Value;
                        }
                    }
                }, fields);
                _owner.Entries.Enqueue(new LogEntry(logLevel, exception, fields));
            }
        }
    }

    private sealed class BrokenWriteStore : IEquipmentWriteStore
    {
        public Task<Result<EquipmentDto>> CreateAsync(
            EquipmentItem item,
            int maximumCount,
            CancellationToken cancellationToken)
        {
            throw new IOException("internal storage details");
        }

        public Task<EquipmentItem?> FindForUpdateAsync(
            int id,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<Result<EquipmentDto>> SaveUpdatedAsync(
            EquipmentItem item,
            long expectedVersion,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }
}
