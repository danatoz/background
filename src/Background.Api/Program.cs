using Background.AI;
using Background.Api.Models;
using Background.Api.Workers;
using Background.Dal;
using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure;
using Background.Infrastructure.Storage;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi("v1", opt =>
{
    opt.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "Background Inbox API";
        doc.Info.Version = "1.0.0";
        doc.Info.Description = "Inbox message processing pipeline with LLM classification";
        return Task.CompletedTask;
    });
});
builder.Services.AddDal(builder.Configuration.GetConnectionString("Postgres")!);
builder.Services.AddInfrastructure();
builder.Services.AddAi(builder.Configuration);
builder.Services.AddHostedService<InboxProcessingWorker>();

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference(opt => opt
    .WithTitle("Background Inbox API")
    .WithTheme(ScalarTheme.Purple)
    .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient));
app.MapGet("/scalar", () => Results.Redirect("/scalar/v1"));

app.MapGet("/health", async (IUnitOfWork uow, CancellationToken ct) =>
{
    try
    {
        var canConnect = await uow.CanConnectAsync(ct);
        return canConnect
            ? Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow })
            : Results.Problem(statusCode: 503, title: "Database unavailable");
    }
    catch (Exception ex)
    {
        return Results.Problem(detail: ex.Message, statusCode: 503, title: "Service Unavailable");
    }
})
.WithTags("Health")
.WithName("HealthCheck")
.WithSummary("Health check endpoint")
.WithDescription("Returns service health status. Checks database connectivity.");

app.MapGet("/messages", async (
    IInboxMessageRepository repo,
    CancellationToken ct,
    string? status = null,
    DateTime? createdFrom = null,
    DateTime? createdTo = null,
    int offset = 0,
    int limit = 20) =>
{
    MessageStatus? statusFilter = null;
    if (status is not null)
    {
        if (!Enum.TryParse<MessageStatus>(status, ignoreCase: true, out var parsed))
            return Results.BadRequest(new { error = $"Invalid status. Valid: {string.Join(", ", Enum.GetNames<MessageStatus>())}" });
        statusFilter = parsed;
    }

    if (limit is < 1 or > 100)
        limit = 20;
    if (offset < 0)
        offset = 0;

    var result = await repo.GetListAsync(
        statusFilter, createdFrom, createdTo, offset, limit, ct);

    return Results.Ok(new
    {
        items = result.Items.Select(MessageResponse.From),
        total = result.Total,
        offset,
        limit
    });
})
.WithTags("Messages")
.WithName("GetMessages")
.WithSummary("List messages with filtering and pagination")
.WithDescription("Returns paginated list of inbox messages with optional status and date filters.");

app.MapPost("/messages", async (
    CreateMessageRequest request,
    IInboxMessageRepository repo,
    IUnitOfWork uow,
    CancellationToken ct) =>
{
    if (!request.IsValid())
        return Results.BadRequest(new { error = "Payload is required" });

    var message = new InboxMessage
    {
        Id = Guid.NewGuid(),
        Payload = request.Payload,
        Status = MessageStatus.Pending,
        RetryCount = 0,
        CreatedAt = DateTime.UtcNow
    };

    await repo.AddAsync(message, ct);
    await uow.SaveChangesAsync(ct);

    return Results.Created($"/messages/{message.Id}", MessageResponse.From(message));
})
.WithTags("Messages")
.WithName("CreateMessage")
.WithSummary("Create a new message")
.WithDescription("Accepts a new message payload and queues it for processing.");

app.MapGet("/prompts", async (
    PromptService promptService,
    CancellationToken ct) =>
{
    var prompts = await promptService.GetAllAsync(ct);
    return Results.Ok(prompts.Select(PromptSummaryResponse.From));
})
.WithTags("Prompts")
.WithName("GetPrompts")
.WithSummary("List all prompts")
.WithDescription("Returns all prompts without Content and SystemPrompt fields.");

app.MapGet("/prompts/{id:guid}", async (
    Guid id,
    PromptService promptService,
    CancellationToken ct) =>
{
    var prompt = await promptService.GetByIdAsync(id, ct);
    if (prompt is null)
        return Results.NotFound(new { error = "Prompt not found" });

    return Results.Ok(PromptDetailResponse.From(prompt));
})
.WithTags("Prompts")
.WithName("GetPromptById")
.WithSummary("Get prompt by ID")
.WithDescription("Returns full prompt details including Content and SystemPrompt.");

app.MapPost("/prompts", async (
    CreatePromptRequest request,
    PromptService promptService,
    CancellationToken ct) =>
{
    var prompt = new Prompt
    {
        Name = request.Name,
        Version = request.Version,
        Content = request.Content,
        SystemPrompt = request.SystemPrompt,
        ModelName = request.ModelName,
        Temperature = request.Temperature,
        IsActive = request.IsActive,
    };

    var created = await promptService.CreateAsync(prompt, ct);
    return Results.Created($"/prompts/{created.Id}", PromptDetailResponse.From(created));
})
.WithTags("Prompts")
.WithName("CreatePrompt")
.WithSummary("Create a new prompt")
.WithDescription("Creates a new prompt template for LLM classification.");

app.MapPut("/prompts/{id:guid}", async (
    Guid id,
    UpdatePromptRequest request,
    PromptService promptService,
    CancellationToken ct) =>
{
    var existing = await promptService.GetByIdAsync(id, ct);
    if (existing is null)
        return Results.NotFound(new { error = "Prompt not found" });

    var updated = new Prompt
    {
        Name = request.Name,
        Version = request.Version,
        Content = request.Content,
        SystemPrompt = request.SystemPrompt,
        ModelName = request.ModelName,
        Temperature = request.Temperature,
        IsActive = request.IsActive,
    };

    var result = await promptService.UpdateAsync(existing, updated, ct);
    return Results.Ok(PromptDetailResponse.From(result));
})
.WithTags("Prompts")
.WithName("UpdatePrompt")
.WithSummary("Update a prompt")
.WithDescription("Fully replaces a prompt template by ID.");

app.MapPost("/messages/{id:guid}/restart", async (
    Guid id,
    IInboxMessageRepository repo,
    CancellationToken ct) =>
{
    var message = await repo.GetByIdAsync(id, ct);
    if (message is null)
        return Results.NotFound(new { error = "Message not found" });

    await repo.ResetToPendingAsync(id, ct);

    return Results.Ok(new { id, status = "restarted" });
})
.WithTags("Messages")
.WithName("RestartMessage")
.WithSummary("Restart message processing")
.WithDescription("Resets a message to pending status so the worker re-processes it from scratch.");

app.Run();
