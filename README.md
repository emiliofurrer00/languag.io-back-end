# Languag.io API

Languag.io API is the backend service for the Languag.io language-learning app. It powers decks, cards, study sessions, public profiles, friends, feed activity, notifications, Kinde user synchronization, and custom profile-picture uploads backed by Amazon S3 and CloudFront.

## Stack

| Layer | Technology |
| --- | --- |
| Runtime | .NET 10 / ASP.NET Core |
| API | Controllers, JWT bearer auth, Swagger |
| Application | Feature services, DTOs, repository contracts |
| Domain | Entity models and enums |
| Persistence | Entity Framework Core, PostgreSQL, Npgsql |
| File storage | Amazon S3 presigned POST uploads |
| CDN | Amazon CloudFront |
| Auth | Kinde JWT access tokens and Kinde webhooks |
| Deployment | Docker, Railway |
| Tests | xUnit, EF Core SQLite test contexts |

## Project Structure

```text
src/
  Languag.io.Api/             HTTP controllers, auth setup, contracts, webhooks
  Languag.io.Application/     Use-case services, DTOs, interfaces
  Languag.io.Domain/          Core entities and enums
  Languag.io.Infrastructure/  EF Core, repositories, migrations, S3 storage
docs/
  ARCHITECTURE.md             Layering, runtime flows, and project boundaries
  ENTITIES_AND_MIGRATIONS.md  Entity, migration, and local database workflow
tests/
  Languag.io.Tests/           Unit and integration-style tests
```

See [Architecture](./docs/ARCHITECTURE.md) for a deeper explanation of the layering and runtime flows. See [Entities And Migrations](./docs/ENTITIES_AND_MIGRATIONS.md) for the local EF Core workflow when adding tables, relationships, indexes, seed data, or data backfills.

## Getting Started

### Full Local Dev Environment

Use this flow when you want the API, worker, and frontend running together.

One-time setup from the backend repository root:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back
dotnet restore
```

If `dotnet ef` is not available, install it once:

```powershell
dotnet tool install --global dotnet-ef
```

Start or confirm local PostgreSQL is available on `localhost:5433`. The default local connection string is:

```text
Host=localhost;Port=5433;Database=languagio;Username=postgres;Password=postgres
```

If you need a quick local PostgreSQL container:

```powershell
docker run --name languagio-postgres -e POSTGRES_DB=languagio -e POSTGRES_USER=postgres -e POSTGRES_PASSWORD=postgres -p 5433:5432 -d postgres:16
```

Apply migrations. From the repository root:

```powershell
dotnet ef database update --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

Or, from `src/`:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back\src
dotnet ef database update --project Languag.io.Infrastructure --startup-project Languag.io.Api
```

`src/Languag.io.Api/appsettings.Development.json` currently has `ApplyMigrationsOnStartup` set to `true`, so the API also applies pending migrations when it starts in local development. Running the command explicitly is still useful when you want to check migration errors before launching the API.

Set API user secrets:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back\src\Languag.io.Api

dotnet user-secrets set "AI_PROVIDER" "OpenAI"
dotnet user-secrets set "OPENAI_API_KEY" "..."
dotnet user-secrets set "AudioStorage:AccessKeyId" "..."
dotnet user-secrets set "AudioStorage:SecretAccessKey" "..."
dotnet user-secrets set "AudioStorage:BucketName" "..."
dotnet user-secrets set "AudioStorage:Region" "eu-north-1"
dotnet user-secrets set "AudioStorage:PublicBaseUrl" "https://dxxxxxxxxxxxxx.cloudfront.net"
dotnet user-secrets set "AudioStorage:KeyPrefix" "audio/tts"
```

Set Worker user secrets too. The worker is the process that calls OpenAI and uploads MP3 files:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back\src\Languag.io.Worker

dotnet user-secrets set "AI_PROVIDER" "OpenAI"
dotnet user-secrets set "OPENAI_API_KEY" "..."
dotnet user-secrets set "AudioStorage:AccessKeyId" "..."
dotnet user-secrets set "AudioStorage:SecretAccessKey" "..."
dotnet user-secrets set "AudioStorage:BucketName" "..."
dotnet user-secrets set "AudioStorage:Region" "eu-north-1"
dotnet user-secrets set "AudioStorage:PublicBaseUrl" "https://dxxxxxxxxxxxxx.cloudfront.net"
dotnet user-secrets set "AudioStorage:KeyPrefix" "audio/tts"
```

Run the dev environment in three terminals.

Terminal 1, API:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back\src
dotnet run --project Languag.io.Api
```

Terminal 2, Worker:

```powershell
cd C:\Users\emili\OneDrive\Escritorio\languag.io_back\src
dotnet run --project Languag.io.Worker
```

Terminal 3, frontend:

```powershell
cd "C:\Users\emili\OneDrive\Escritorio\languag.io front"
yarn dev
```

The development launch profile serves the API at:

```text
http://localhost:5222
```

Swagger is enabled in development:

```text
http://localhost:5222/swagger
```

The frontend usually runs at:

```text
http://localhost:3000
```

To test AI deck audio locally, create an AI deck from the frontend with audio enabled. The worker should process the job, upload MP3 files under `audio/tts/...`, and flashcards should show a volume button in study mode once `frontAudioUrl` is returned by the API.

## Configuration

The API reads configuration from `appsettings.json`, environment variables, and user secrets in local development. ASP.NET Core maps double underscores to nested config keys, so `ConnectionStrings__DefaultConnection` maps to `ConnectionStrings:DefaultConnection`.

### Required Core Settings

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Authentication__Kinde__Authority` | Kinde issuer/authority URL |
| `Authentication__Kinde__Audience` | Kinde API audience expected in access tokens |

### Profile Image Settings

| Setting | Purpose |
| --- | --- |
| `AWS_ACCESS_KEY_ID` | AWS access key used by the API to sign upload policies and validate/delete objects |
| `AWS_SECRET_ACCESS_KEY` | AWS secret access key |
| `AWS_SESSION_TOKEN` | Optional session token for temporary credentials |
| `AWS_REGION` | S3 bucket region |
| `AWS_S3_BUCKET` | S3 bucket used for profile-picture objects |
| `CLOUDFRONT_BASE_URL` | Public CloudFront base URL used when returning profile image URLs |
| `ProfileImages__KeyPrefix` | S3 object prefix, defaults to `profile-pictures` |
| `ProfileImages__MaxUploadBytes` | Maximum accepted compressed image size, defaults to `524288` |
| `ProfileImages__UploadTtlSeconds` | Presigned upload policy lifetime, defaults to `120` |

Local user-secrets example:

```powershell
cd src\Languag.io.Api

dotnet user-secrets init
dotnet user-secrets set "ProfileImages:AccessKeyId" "..."
dotnet user-secrets set "ProfileImages:SecretAccessKey" "..."
dotnet user-secrets set "ProfileImages:Region" "eu-north-1"
dotnet user-secrets set "ProfileImages:BucketName" "languagio-profile-images-dev"
dotnet user-secrets set "ProfileImages:CloudFrontBaseUrl" "https://dxxxxxxxxxxxxx.cloudfront.net"
```

### AI TTS Audio Settings

AI-generated decks can optionally create reusable front-card MP3 audio assets. The worker reads the same OpenAI key used for deck generation and stores files in S3 or an S3-compatible service such as Cloudflare R2.

| Setting | Purpose |
| --- | --- |
| `OPENAI_API_KEY` / `OpenAI__ApiKey` | OpenAI API key used for deck generation and TTS |
| `OpenAI__TtsModel` | TTS model, defaults to `gpt-4o-mini-tts` |
| `OpenAI__TtsVoice` | TTS voice, defaults to `cedar` |
| `OpenAI__TtsSpeed` | Speech speed, defaults to `0.9` |
| `AudioStorage__AccessKeyId` / `AWS_ACCESS_KEY_ID` | S3/R2 access key |
| `AudioStorage__SecretAccessKey` / `AWS_SECRET_ACCESS_KEY` | S3/R2 secret key |
| `AudioStorage__BucketName` / `AWS_S3_BUCKET` | Bucket for generated MP3 files |
| `AudioStorage__Region` / `AWS_REGION` | Bucket region |
| `AudioStorage__ServiceUrl` | Optional S3-compatible endpoint URL for R2 |
| `AudioStorage__PublicBaseUrl` / `AUDIO_CDN_BASE_URL` | Public CDN/base URL for audio playback |
| `AudioStorage__KeyPrefix` | Object prefix, defaults to `audio/tts` |

## Profile Picture Upload Flow

Profile pictures are stored as private S3 objects and served publicly through CloudFront.

1. The frontend compresses a selected image to `256x256` WebP.
2. The frontend calls `POST /api/Users/me/profile-picture/upload-request`.
3. The API validates content type and size, then returns a short-lived S3 presigned POST target.
4. The browser uploads the WebP directly to S3.
5. The frontend calls `POST /api/Users/me/profile-picture/complete` with the object key.
6. The API validates object ownership, S3 metadata, content type, and size.
7. The API stores the object key on `User.ProfilePictureObjectKey`.
8. Profile, feed, friend, and notification DTOs return CloudFront URLs derived from the object key.

The database stores the object key, not the full CDN URL. This keeps CDN/domain changes configurable.

## AWS Notes

Use a bucket name without dots so browser uploads do not hit S3 TLS wildcard-certificate issues.

The S3 bucket should remain private. CloudFront should use Origin Access Control with signed origin requests. The bucket policy should allow `s3:GetObject` from the CloudFront distribution for `profile-pictures/*`.

S3 CORS must allow `POST` from frontend origins because the browser uploads directly to S3:

```json
[
  {
    "AllowedHeaders": ["*"],
    "AllowedMethods": ["POST"],
    "AllowedOrigins": ["http://localhost:3000", "https://your-frontend-domain.com"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```

## Railway Deployment

The repository includes a root `Dockerfile` that publishes and runs `src/Languag.io.Api`.

Recommended Railway API variables:

```text
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:80
ConnectionStrings__DefaultConnection=Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require;Trust Server Certificate=true
AWS_ACCESS_KEY_ID=...
AWS_SECRET_ACCESS_KEY=...
AWS_REGION=...
AWS_S3_BUCKET=...
CLOUDFRONT_BASE_URL=https://dxxxxxxxxxxxxx.cloudfront.net
```

EF Core migrations run during API startup in `Program.cs`.

## Useful Commands

```powershell
dotnet build src\Languag.io.Api\Languag.io.Api.csproj
dotnet test tests\Languag.io.Tests\Languag.io.Tests.csproj
dotnet ef database update --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
dotnet ef migrations add AddBadges --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
dotnet ef migrations list --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

See [Entities And Migrations](./docs/ENTITIES_AND_MIGRATIONS.md) for the full workflow, including adding entities, reviewing generated migrations, updating local PostgreSQL, rolling back local migrations, and writing safe backfills.

## Operational Notes

- Keep AWS credentials server-side only. The frontend never receives AWS keys.
- Keep `Authentication__Kinde__Audience` aligned with the frontend `KINDE_AUDIENCE`.
- If CloudFront image URLs return `403`, check the object key, CloudFront origin, OAC signing, bucket policy, and encryption settings.
- The current upload path expects frontend-side WebP compression. A future Lambda/image-processing pipeline can replace or supplement that step.
