using System.Diagnostics;
using System.Text.Json;
using Background.AI;
using Background.Api.Models;
using Background.Api.Workers;
using Background.Dal;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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
builder.Services.AddHostedService<JobProcessingWorker>();

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

app.MapGet("/health/llm", async (HttpContext http, CancellationToken ct) =>
{
    var kernel = http.RequestServices.GetRequiredService<Kernel>();
    var chat = kernel.GetRequiredService<IChatCompletionService>();
    var sw = Stopwatch.StartNew();
    try
    {
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage("ping");
        var result = await chat.GetChatMessageContentAsync(
            chatHistory, new OpenAIPromptExecutionSettings { MaxTokens = 1 }, null, ct);
        sw.Stop();
        return Results.Ok(new
        {
            status = "healthy",
            model = result.ModelId,
            latencyMs = (int)sw.Elapsed.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        });
    }
    catch (Exception ex)
    {
        sw.Stop();
        return Results.Problem(
            detail: ex.Message,
            statusCode: 503,
            title: "LLM unavailable");
    }
})
.WithTags("Health")
.WithName("LlmHealthCheck")
.WithSummary("LLM health check endpoint")
.WithDescription("Sends a minimal ping to the LLM and returns model, latency and status.");

app.MapGet("/jobs", async (
    IProcessingJobRepository repo,
    CancellationToken ct,
    string? status = null,
    string? senderName = null,
    string? senderAddress = null,
    string? folder = null,
    DateTime? createdFrom = null,
    DateTime? createdTo = null,
    int offset = 0,
    int limit = 20) =>
{
    JobStatus? statusFilter = null;
    if (status is not null)
    {
        if (!Enum.TryParse<JobStatus>(status, ignoreCase: true, out var parsed))
            return Results.BadRequest(new { error = $"Invalid status. Valid: {string.Join(", ", Enum.GetNames<JobStatus>())}" });
        statusFilter = parsed;
    }

    if (limit is < 1 or > 100)
        limit = 20;
    if (offset < 0)
        offset = 0;

    var result = await repo.GetListAsync(
        statusFilter, senderName, senderAddress, folder, createdFrom, createdTo, offset, limit, ct);

    return Results.Ok(new
    {
        items = result.Items.Select(JobResponse.From),
        total = result.Total,
        offset,
        limit
    });
})
.WithTags("Jobs")
.WithName("GetJobs")
.WithSummary("List processing jobs with filtering and pagination")
.WithDescription("Returns paginated list of processing jobs with optional status and date filters.");

app.MapPost("/jobs", async (
    CreateJobRequest request,
    IProcessingJobRepository repo,
    IUnitOfWork uow,
    IStorageService storage,
    CancellationToken ct) =>
{
    if (!request.IsValid())
        return Results.BadRequest(new { error = "Payload is required" });

    EmailPayload? parsed;
    try
    {
        parsed = JsonSerializer.Deserialize<EmailPayload>(request.Payload);
    }
    catch (JsonException)
    {
        return Results.BadRequest(new { error = "Payload must be a valid JSON" });
    }

    if (parsed is null)
        return Results.BadRequest(new { error = "Payload must be a valid JSON" });

    var messageId = Guid.NewGuid();
    var prefix = ArtifactPathBuilder.BuildPrefix("emails", messageId);
    var rawKey = ArtifactPathBuilder.Raw(prefix);

    await storage.SaveAsync(rawKey, request.Payload, "application/json", ct);

    var message = new ProcessingJob
    {
        Id = messageId,
        ArtifactPrefix = prefix,
        Status = JobStatus.Pending,
        RetryCount = 0,
        CreatedAt = DateTime.UtcNow,
        EmailMetadata = new EmailMetadata
        {
            Id = messageId,
            SenderName = parsed.SenderName,
            SenderAddress = parsed.SenderAddress,
            Folder = parsed.Folder,
            BodyIsHtml = parsed.Body?.IsHtml,
            BodyS3Key = parsed.Body?.S3Key,
            AttachmentsJson = parsed.Attachments is { Count: > 0 }
                ? JsonSerializer.Serialize(parsed.Attachments)
                : null
        }
    };

    await repo.AddAsync(message, ct);
    await uow.SaveChangesAsync(ct);

    return Results.Created($"/jobs/{message.Id}", JobResponse.From(message));
})
.WithTags("Jobs")
.WithName("CreateJob")
.WithSummary("Create a new processing job")
.WithDescription("Accepts a payload and queues it for processing.");

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
            MaxTokens = request.MaxTokens,
            ResponseFormat = request.ResponseFormat,
            TopP = request.TopP,
            Seed = request.Seed,
            Description = request.Description,
            Tags = request.Tags,
            ResponseSchema = request.ResponseSchema,
            Provider = request.Provider ?? "ChatCompletion",
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
            MaxTokens = request.MaxTokens,
            ResponseFormat = request.ResponseFormat,
            TopP = request.TopP,
            Seed = request.Seed,
            Description = request.Description,
            Tags = request.Tags,
            ResponseSchema = request.ResponseSchema,
            Provider = request.Provider ?? "ChatCompletion",
            IsActive = request.IsActive,
        };

    var result = await promptService.UpdateAsync(existing, updated, ct);
    return Results.Ok(PromptDetailResponse.From(result));
})
.WithTags("Prompts")
.WithName("UpdatePrompt")
.WithSummary("Update a prompt")
.WithDescription("Fully replaces a prompt template by ID.");

app.MapPost("/jobs/{id:guid}/restart", async (
    Guid id,
    IProcessingJobRepository repo,
    CancellationToken ct) =>
{
    var message = await repo.GetByIdAsync(id, ct);
    if (message is null)
        return Results.NotFound(new { error = "Job not found" });

    await repo.ResetToPendingAsync(id, ct);

    return Results.Ok(new { id, status = "restarted" });
})
.WithTags("Jobs")
.WithName("RestartJob")
.WithSummary("Restart job processing")
.WithDescription("Resets a job to pending status so the worker re-processes it from scratch.");

app.MapGet("/jobs/{id:guid}", async (
    Guid id,
    IProcessingJobRepository repo,
    CancellationToken ct) =>
{
    var message = await repo.GetByIdAsync(id, ct);
    if (message is null)
        return Results.NotFound(new { error = "Job not found" });

    return Results.Ok(JobDetailResponse.From(message));
})
.WithTags("Jobs")
.WithName("GetJobById")
.WithSummary("Get job details with artifacts")
.WithDescription("Returns a single job with its available artifact files.");

app.MapGet("/jobs/{id:guid}/artifacts/{fileName}", async (
    Guid id,
    string fileName,
    IProcessingJobRepository repo,
    IStorageService storage,
    CancellationToken ct) =>
{
    var message = await repo.GetByIdAsync(id, ct);
    if (message is null)
        return Results.NotFound(new { error = "Job not found" });

    if (message.ArtifactPrefix is null)
        return Results.NotFound(new { error = "No artifacts available. Job has not been processed yet." });

    var key = fileName switch
    {
        "raw.json" => ArtifactPathBuilder.Raw(message.ArtifactPrefix),
        "preprocessed.md" => ArtifactPathBuilder.Preprocessed(message.ArtifactPrefix),
        "prompt.md" => ArtifactPathBuilder.Prompt(message.ArtifactPrefix),
        "response.json" => ArtifactPathBuilder.LlmResponse(message.ArtifactPrefix),
        "processed.json" => ArtifactPathBuilder.Processed(message.ArtifactPrefix),
        _ => null
    };

    if (key is null)
        return Results.BadRequest(new { error = $"Unknown artifact: '{fileName}'. Valid: raw.json, preprocessed.md, prompt.md, response.json, processed.json" });

    var content = await storage.GetAsync(key, ct);
    if (content is null)
        return Results.NotFound(new { error = "Artifact not found in storage" });

    var contentType = fileName switch
    {
        "raw.json" => "application/json; charset=utf-8",
        "preprocessed.md" => "text/plain; charset=utf-8",
        "prompt.md" => "text/plain; charset=utf-8",
        "response.json" => "application/json; charset=utf-8",
        "processed.json" => "application/json; charset=utf-8",
        _ => "application/octet-stream"
    };

    return Results.Content(content, contentType);
})
.WithTags("Jobs")
.WithName("GetJobArtifact")
.WithSummary("Get job artifact content")
.WithDescription("Returns the content of a specific processing artifact by file name.");

app.Run();
