WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
string connectionString = builder.Configuration.GetConnectionString("Equipment")
    ?? throw new InvalidOperationException("ConnectionStrings:Equipment must be configured.");

builder.Services.AddEquipmentInfrastructure(connectionString);
builder.Services.AddEquipmentApplication();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var problem = new ValidationProblemDetails(context.ModelState)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = ProblemDetailsConstants.ValidationTitle
        };
        problem.Extensions[ProblemDetailsConstants.Code] = ProblemDetailsConstants.ValidationCode;
        problem.Extensions[ProblemDetailsConstants.TraceId] = context.HttpContext.TraceIdentifier;
        return new BadRequestObjectResult(problem) { ContentTypes = { ProblemDetailsConstants.MediaType } };
    };
});
builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
{
    context.ProblemDetails.Extensions[ProblemDetailsConstants.TraceId] = context.HttpContext.TraceIdentifier;
    if (context.ProblemDetails.Status >= StatusCodes.Status500InternalServerError)
    {
        context.ProblemDetails.Extensions.TryAdd(ProblemDetailsConstants.Code, ProblemDetailsConstants.UnexpectedErrorCode);
    }
});
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseReadinessCheck>("database", tags: [HealthCheckTags.Readiness], timeout: TimeSpan.FromSeconds(3));

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    DatabaseBootstrapper bootstrapper = scope.ServiceProvider.GetRequiredService<DatabaseBootstrapper>();
    if (builder.Configuration.GetValue<bool>("Database:MigrateOnly"))
    {
        await bootstrapper.MigrateAsync(app.Lifetime.ApplicationStopping);
        return;
    }

    await bootstrapper.SeedTypesAsync(app.Lifetime.ApplicationStopping);
}

app.UseMiddleware<RequestLoggingScopeMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapControllers();
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(HealthCheckTags.Readiness) });

app.Run();

/// <summary>
/// Открывает точку входа приложения для интеграционного тестового хоста
/// </summary>
public partial class Program
{
}
