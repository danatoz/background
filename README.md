# Backgroud Inbox

> Message processing pipeline with LLM classification. Accepts raw inbox messages, runs them through a multi-step pipeline (storage → preprocessing → LLM classification → validation → artifact storage), and tracks status in PostgreSQL.

## Overview

Backgroud Inbox is a .NET 10 web API that ingests messages (e.g., email bodies, notifications) and classifies them via an LLM. Each message flows through a resumable pipeline with artifacts stored in S3-compatible storage (MinIO). Built with Clean Architecture, EF Core, Semantic Kernel, and ASP.NET Core Minimal APIs.

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://docs.docker.com/get-docker/) (for PostgreSQL + MinIO)

## Quick Start

```bash
# 1. Start infrastructure
docker compose up -d

# 2. Apply EF Core migrations
dotnet ef database update --project src/Background.Dal --startup-project src/Background.Api

# 3. Run the API
dotnet run --project src/Background.Api
```

API is available at `http://localhost:5293`. Scalar API reference at `/scalar`.

## Configuration

| Section | Key | Default | Description |
|---|---|---|---|
| `ConnectionStrings:Postgres` | — | `Host=localhost;Port=5433;...` | PostgreSQL connection string |
| `S3:ServiceUrl` | — | `http://127.0.0.1:9000` | S3/MinIO endpoint |
| `S3:BucketName` | — | `inbox` | S3 bucket for artifacts |
| `S3:AccessKey` | — | `minioadmin` | S3 access key |
| `S3:SecretKey` | — | `minioadmin` | S3 secret key |
| `Llm:Endpoint` | — | `http://localhost:8080/v1` | OpenAI-compatible API endpoint |
| `Llm:ModelId` | — | `gpt-4o-mini` | LLM model name |
| `Llm:ApiKey` | — | `""` | API key (empty for local) |
| `Llm:Temperature` | — | `0.0` | LLM temperature |
| `Llm:MaxTokens` | — | `8192` | Max output tokens |
| `Llm:PromptName` | — | `inbox-classification` | Active prompt template name |

## API Endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/messages` | List messages with filtering and pagination |
| `POST` | `/messages` | Create a new message (queues for processing) |
| `GET` | `/messages/{id}/restart` | Reset a failed message to pending |
| `GET` | `/prompts` | List all prompt templates |
| `POST` | `/prompts` | Create a new prompt template |
| `GET` | `/prompts/{id}` | Get prompt template details |
| `PUT` | `/prompts/{id}` | Update a prompt template |
| `GET` | `/health` | Health check (DB connectivity) |
| `GET` | `/scalar` | API reference UI |

### Create a message

```bash
curl -X POST http://localhost:5293/messages \
  -H "Content-Type: application/json" \
  -d '{"payload": "Invoice from vendor for $1,200"}'
```

## Project Structure

```
src/
├── Background.Api/          # Web API host (Minimal APIs, workers)
├── Background.AI/           # LLM integration (Semantic Kernel)
├── Background.Dal/          # Data access (EF Core, PostgreSQL, repositories)
└── Background.Infrastructure/  # Pipeline orchestration, artifact storage (S3)
```

## Pipeline Steps

Each message goes through 5 sequential steps:

1. **RawStorageStep** — Save raw payload to S3
2. **PreprocessingStep** — Strip HTML, collapse whitespace
3. **LlmStep** — Classify via LLM with active prompt template
4. **ValidationStep** — Validate JSON response (summary + category)
5. **CompleteStep** — Save processed artifact to S3

Failed messages are retried with exponential backoff (`2^min(retry,5) * 10s`).

## Testing

```bash
# Send test messages
./test.sh [count]

# Run pipeline directly (requires dotnet)
dotnet run --project src/Background.Api
```
