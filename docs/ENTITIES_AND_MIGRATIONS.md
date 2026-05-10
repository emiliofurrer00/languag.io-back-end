# Entities And Migrations

This guide covers the local workflow for adding durable data to Languag.io: domain entities, EF Core mappings, migrations, local database updates, seed/reference data, and data backfills.

## Where Things Live

| Concern | Location |
| --- | --- |
| Domain entities and enums | `src/Languag.io.Domain` |
| Application DTOs, commands, services, repository interfaces | `src/Languag.io.Application` |
| EF Core `DbContext`, entity mappings, repositories, migrations | `src/Languag.io.Infrastructure` |
| HTTP controllers and request/response contracts | `src/Languag.io.Api` |
| Tests | `tests/Languag.io.Tests` |

`AppDbContext` is in `src/Languag.io.Infrastructure/Persistence/AppDbContext.cs`. EF Core migrations are generated into `src/Languag.io.Infrastructure/Migrations`.

## Add Or Change An Entity

Use this checklist when adding something like badges, achievements, user badge grants, or any other persisted feature.

1. Add the domain type in `src/Languag.io.Domain/Entities`.
2. Add navigation properties on related entities only when they make queries or aggregate logic clearer.
3. Add a `DbSet<T>` to `AppDbContext`.
4. Add EF mapping for keys, required fields, max lengths, defaults, indexes, and delete behavior.
5. Add application DTOs and repository/service contracts.
6. Implement the repository in Infrastructure.
7. Expose the use case through an API controller if the frontend needs it.
8. Add tests for business rules and repository behavior.
9. Generate and review an EF Core migration.
10. Apply the migration to the local database.

Small mappings can live inline in `AppDbContext`, matching much of the current codebase. For larger new features, prefer an `IEntityTypeConfiguration<T>` class under `src/Languag.io.Infrastructure/Persistence/Configurations`; `AppDbContext` already calls `ApplyConfigurationsFromAssembly`.

## Example Shape: Badges

For a badge catalog plus user-owned awards, use two tables:

```text
Badges
- Id
- Code
- Name
- Description
- Category
- ImageObjectKey
- IsActive
- SortOrder
- CreatedAtUtc
- UpdatedAtUtc

UserBadges
- UserId
- BadgeId
- AwardedAtUtc
- IsVisible
- IsPinned
- Source
- Metadata
```

Prefer `UserBadges` over names like `UserBadgeBridges`. The row is not just a bridge; it is a user-owned award with visibility, timestamps, and optional metadata.

Use a unique key on `(UserId, BadgeId)` so awarding is idempotent. For public assets, store an object key such as `badges/founding-user.webp`, not a full CDN URL. Build the public URL at read time from configuration, the same way profile pictures store `ProfilePictureObjectKey` and expose a CloudFront URL in DTOs.

## Create A Migration

Install the EF Core CLI once if needed:

```powershell
dotnet tool install --global dotnet-ef
```

From the repository root:

```powershell
dotnet ef migrations add AddBadges --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

The Infrastructure project owns `AppDbContext` and migrations. The API project is the startup project because it supplies runtime configuration and dependency injection.

After generating a migration, review:

```text
src/Languag.io.Infrastructure/Migrations/<timestamp>_AddBadges.cs
src/Languag.io.Infrastructure/Migrations/<timestamp>_AddBadges.Designer.cs
src/Languag.io.Infrastructure/Migrations/AppDbContextModelSnapshot.cs
```

Check that the generated migration includes the expected tables, columns, indexes, foreign keys, defaults, and delete behavior. If the generated migration surprises you, fix the entity mapping and regenerate before applying it.

If you generated a migration locally but have not applied or shared it yet, remove it with:

```powershell
dotnet ef migrations remove --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

## Apply Migrations Locally

Start local PostgreSQL on `localhost:5433`. The default local connection string is in `src/Languag.io.Api/appsettings.Development.json`:

```text
Host=localhost;Port=5433;Database=languagio;Username=postgres;Password=postgres
```

Apply pending migrations:

```powershell
dotnet ef database update --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

List migrations and confirm what EF sees:

```powershell
dotnet ef migrations list --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

The API also applies migrations on startup when `ApplyMigrationsOnStartup` is `true`. In local development that is convenient, but explicit `dotnet ef database update` commands are better when you are actively authoring and debugging schema changes.

## Roll Back A Local Migration

To move the local database back to a previous migration:

```powershell
dotnet ef database update PreviousMigrationName --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

Then remove the newest unapplied migration from the codebase if it should not exist:

```powershell
dotnet ef migrations remove --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api
```

Only do this for local, unshared work. Once a migration has been pushed or applied outside your machine, prefer adding a new corrective migration.

## Seed Data And Backfills

Use migrations for reference data that should deploy with the schema, such as badge definitions:

```csharp
var badgeId = new Guid("11111111-1111-1111-1111-111111111111");
var createdAtUtc = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);

migrationBuilder.InsertData(
    table: "Badges",
    columns: new[] { "Id", "Code", "Name", "Description", "ImageObjectKey", "IsActive", "SortOrder", "CreatedAtUtc", "UpdatedAtUtc" },
    values: new object[] { badgeId, "founding_user", "Founding User", "One of the first users of Languag.io.", "badges/founding-user.webp", true, 10, createdAtUtc, createdAtUtc });
```

Use real stable IDs for seeded records. Do not call `Guid.NewGuid()` inside the migration because the value should be deterministic across environments.

A backfill is a retroactive data grant for users who already meet criteria. Keep backfills idempotent so they can be rerun safely.

For a simple historical badge, SQL inside a migration can be enough:

```sql
INSERT INTO "UserBadges" ("UserId", "BadgeId", "AwardedAtUtc", "IsVisible", "Source")
SELECT first_users."Id", badges."Id", NOW(), true, 'Backfill'
FROM (
    SELECT "Id"
    FROM "Users"
    ORDER BY "CreatedAtUtc" ASC, "Id" ASC
    LIMIT 10
) first_users
JOIN "Badges" badges ON badges."Code" = 'founding_user'
ON CONFLICT ("UserId", "BadgeId") DO NOTHING;
```

For complex criteria that reuse application rules, prefer an app-level backfill job or admin command that calls the same service used by live awarding logic.

## Generate A SQL Script

For deployment review or manual database work:

```powershell
dotnet ef migrations script --project src\Languag.io.Infrastructure --startup-project src\Languag.io.Api --idempotent
```

Use idempotent scripts when the target database may be at different migration levels.

## Practical Review Checklist

- Entity and table names are clear and stable.
- String lengths and required fields are explicit.
- Foreign keys specify intended delete behavior.
- Frequently filtered or joined columns have indexes.
- Unique rules are enforced in the database, not only in services.
- Data backfills are deterministic and idempotent.
- Asset fields store object keys, not environment-specific public URLs.
- Tests cover important business rules and repository queries.
- The migration and model snapshot contain only intended changes.
