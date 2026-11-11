namespace Equipment.UnitTests;

public sealed class PipelineBehaviorTests
{
    [Fact]
    public async Task ValidationAggregatesFailuresAndNeverRunsHandler()
    {
        var first = new InlineValidator<GetEquipmentByIdQuery>();
        first.RuleFor(query => query.Id).GreaterThan(0).WithMessage("positive").OverridePropertyName("id");
        var second = new InlineValidator<GetEquipmentByIdQuery>();
        second.RuleFor(query => query.Id).GreaterThan(1).WithMessage("greater than one").OverridePropertyName("id");
        var behavior = new ValidationBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>([first, second]);
        bool handled = false;

        RequestValidationException exception = await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(
            new GetEquipmentByIdQuery(0),
            _ => { handled = true; return Task.FromResult(Result.Failure<EquipmentDto>(EquipmentErrors.NotFound)); },
            CancellationToken.None));

        Assert.Equal(new[] { "positive", "greater than one" }, exception.Errors["id"]);
        Assert.False(handled);
    }

    [Fact]
    public async Task AsyncValidationIsAwaitedAndCancellationReachesValidatorAndHandler()
    {
        using var cancellation = new CancellationTokenSource();
        var validator = new InlineValidator<GetEquipmentByIdQuery>();
        bool validated = false;
        validator.RuleFor(query => query.Id).MustAsync(async (_, token) =>
        {
            Assert.Equal(cancellation.Token, token);
            await Task.Yield();
            validated = true;
            return true;
        });
        var behavior = new ValidationBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>([validator]);

        await behavior.Handle(new GetEquipmentByIdQuery(1), token =>
        {
            Assert.True(validated);
            Assert.Equal(cancellation.Token, token);
            return Task.FromResult(Result.Failure<EquipmentDto>(EquipmentErrors.NotFound));
        }, cancellation.Token);
    }

    [Fact]
    public async Task CancelledValidationNeverRunsHandler()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var behavior = new ValidationBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>([]);
        bool handled = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new GetEquipmentByIdQuery(1), _ =>
        {
            handled = true;
            return Task.FromResult(Result.Failure<EquipmentDto>(EquipmentErrors.NotFound));
        }, cancellation.Token));
        Assert.False(handled);
    }

    [Fact]
    public async Task LoggingPreservesSuccessAndDoesNotRecordPayload()
    {
        var logger = new RecordingLogger<LoggingBehavior<CreateEquipmentCommand, Result<EquipmentDto>>>();
        var behavior = new LoggingBehavior<CreateEquipmentCommand, Result<EquipmentDto>>(logger);
        var command = new CreateEquipmentCommand(new EquipmentDetails("PC1", "private payload", 1, 1));
        var expected = Result.Success(new EquipmentDto(1, "PC1", "private payload", 1, "Type", 1, 1));

        Result<EquipmentDto> actual = await behavior.Handle(command, _ => Task.FromResult(expected), CancellationToken.None);

        Assert.Same(expected, actual);
        Assert.Contains(logger.Entries, entry => entry.Fields.ContainsKey("ElapsedMilliseconds"));
        Assert.All(logger.Entries, entry => Assert.Equal(nameof(CreateEquipmentCommand), entry.Fields["RequestName"]));
        Assert.DoesNotContain(logger.Entries, entry => entry.Message.Contains("private payload", StringComparison.Ordinal));
    }

    [Fact]
    public async Task BusinessRejectionIsNotLoggedAsUnexpectedError()
    {
        var logger = new RecordingLogger<LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>>();
        var behavior = new LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>(logger);
        var expected = Result.Failure<EquipmentDto>(EquipmentErrors.NotFound);

        Assert.Same(expected, await behavior.Handle(new GetEquipmentByIdQuery(1), _ => Task.FromResult(expected), CancellationToken.None));
        Assert.Contains(logger.Entries, entry => entry.Fields.TryGetValue("ErrorCode", out object? code)
            && Equals(code, EquipmentErrors.NotFound.Code));
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Error);
    }

    [Fact]
    public async Task UnexpectedExceptionIsRethrownAndStackIsLeftToHttpBoundary()
    {
        var logger = new RecordingLogger<LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>>();
        var behavior = new LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>(logger);
        var failure = new IOException("storage failure");

        IOException actual = await Assert.ThrowsAsync<IOException>(() => behavior.Handle(new GetEquipmentByIdQuery(1),
            _ => Task.FromException<Result<EquipmentDto>>(failure), CancellationToken.None));

        Assert.Same(failure, actual);
        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Warning);
        Assert.All(logger.Entries, entry => Assert.Null(entry.Exception));
    }

    [Fact]
    public async Task ExpectedValidationExceptionIsLoggedAsValidationOutcome()
    {
        var logger = new RecordingLogger<LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>>();
        var behavior = new LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>(logger);
        var failure = new RequestValidationException(new Dictionary<string, string[]> { ["id"] = ["invalid"] });

        await Assert.ThrowsAsync<RequestValidationException>(() => behavior.Handle(new GetEquipmentByIdQuery(0),
            _ => Task.FromException<Result<EquipmentDto>>(failure), CancellationToken.None));

        Assert.Contains(logger.Entries, entry => entry.Fields.TryGetValue("InvalidFieldCount", out object? count) && Equals(count, 1));
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    [Fact]
    public async Task CancellationIsNotLoggedAsFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var logger = new RecordingLogger<LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>>();
        var behavior = new LoggingBehavior<GetEquipmentByIdQuery, Result<EquipmentDto>>(logger);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => behavior.Handle(new GetEquipmentByIdQuery(1),
            _ => Task.FromCanceled<Result<EquipmentDto>>(cancellation.Token), cancellation.Token));

        Assert.Contains(logger.Entries, entry => entry.Level == LogLevel.Debug);
        Assert.DoesNotContain(logger.Entries, entry => entry.Level >= LogLevel.Warning);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<Entry> Entries { get; } = [];

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
            var fields = ((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary(pair => pair.Key, pair => pair.Value);
            Entries.Add(new Entry(logLevel, formatter(state, exception), exception, fields));
        }
    }

    private sealed record Entry(
        LogLevel Level,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Fields);
}
