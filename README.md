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
tests/
  Languag.io.Tests/           Unit and integration-style tests
```

See [Architecture](./docs/ARCHITECTURE.md) for a deeper explanation of the layering and runtime flows.

## Getting Started

Start a local PostgreSQL instance:

```powershell
docker compose up -d
```

Run the API:

```powershell
dotnet restore
dotnet run --project src\Languag.io.Api
```

The development launch profile serves the API at:

```text
http://localhost:5222
```

Swagger is enabled in development:

```text
http://localhost:5222/swagger
```

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
```

## Operational Notes

- Keep AWS credentials server-side only. The frontend never receives AWS keys.
- Keep `Authentication__Kinde__Audience` aligned with the frontend `KINDE_AUDIENCE`.
- If CloudFront image URLs return `403`, check the object key, CloudFront origin, OAC signing, bucket policy, and encryption settings.
- The current upload path expects frontend-side WebP compression. A future Lambda/image-processing pipeline can replace or supplement that step.
