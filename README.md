# Backgroud Inbox

> Message processing pipeline with LLM classification. Accepts raw payloads, runs them through a multi-step pipeline (preprocessing → LLM classification → validation → artifact storage), and tracks processing status in PostgreSQL. Payloads are stored in S3 (MinIO), not in the database.

## Overview

Backgroud Inbox is a .NET 10 web API that ingests messages (e.g., email bodies, notifications) and classifies them via an LLM. Each job flows through a resumable 4-step pipeline with artifacts stored in S3-compatible storage (MinIO). Built with Clean Architecture, EF Core, Semantic Kernel, and ASP.NET Core Minimal APIs.

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
| `GET` | `/jobs` | List processing jobs with filtering and pagination |
| `POST` | `/jobs` | Create a new processing job (payload saved to S3, queued for pipeline) |
| `POST` | `/jobs/{id}/restart` | Reset a failed job to pending |
| `GET` | `/jobs/{id}` | Get job details with available artifacts |
| `GET` | `/jobs/{id}/artifacts/{fileName}` | Get artifact content (raw.json, preprocessed.md, etc.) |
| `GET/POST/PUT` | `/prompts` | CRUD for LLM prompt templates |
| `GET` | `/health` | Health check (DB connectivity) |
| `GET` | `/scalar` | API reference UI |

### Create a job

```bash
curl -X POST http://localhost:5293/jobs \
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

Each job goes through 4 sequential steps:

1. **PreprocessingStep** — Strip HTML, collapse whitespace
2. **LlmStep** — Classify via LLM with active prompt template
3. **ValidationStep** — Validate JSON response (client info, document type, amount)
4. **CompleteStep** — Save processed artifact to S3

Failed jobs are retried with exponential backoff (`2^min(retry,5) * 10s`).

Payload is saved to S3 on creation (POST `/jobs`) and loaded by the orchestrator before the first pipeline step.

## Testing

```bash
# Send test messages
./test.sh [count]

# Run pipeline directly (requires dotnet)
dotnet run --project src/Background.Api
```
