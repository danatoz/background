using Background.Api.Workers;
using Background.Dal;
using Background.Dal.Entities;
using Background.Dal.Repositories;
using Background.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDal(builder.Configuration.GetConnectionString("Postgres")!);
builder.Services.AddInfrastructure();
builder.Services.AddHostedService<InboxProcessingWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapPost("/messages", async (
    CreateMessageRequest request,
    IInboxMessageRepository repo,
    IUnitOfWork uow,
    CancellationToken ct) =>
{
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

    return Results.Created($"/messages/{message.Id}", message);
})
.WithName("CreateMessage");

app.Run();

public record CreateMessageRequest(string Payload);
