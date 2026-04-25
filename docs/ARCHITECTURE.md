# Architecture

Languag.io API uses a layered .NET architecture. The API layer handles HTTP and auth concerns, the application layer owns use cases and contracts, the domain layer owns core entities, and the infrastructure layer owns persistence and external service integrations.

## System Overview

```text
Browser / Next.js frontend
  -> Kinde authenticated API token
  -> ASP.NET Core controllers
  -> Application services
  -> Repository contracts
  -> EF Core repositories
  -> PostgreSQL

Profile picture upload:
Browser
  -> API presigned upload request
  -> S3 direct upload
  -> API completion validation
  -> PostgreSQL stores object key
  -> CloudFront serves profile image
```

## Layers

| Layer | Location | Responsibility |
| --- | --- | --- |
| API | `src/Languag.io.Api` | Controllers, request/response contracts, auth, rate limiting, Swagger, webhooks |
| Application | `src/Languag.io.Application` | Feature services, DTOs, commands, queries, repository interfaces |
| Domain | `src/Languag.io.Domain` | Entities, enums, relationships, core state |
| Infrastructure | `src/Languag.io.Infrastructure` | EF Core `AppDbContext`, migrations, repository implementations, S3 storage |
| Tests | `tests/Languag.io.Tests` | Service and repository coverage with xUnit |

Dependencies point inward. API and Infrastructure depend on Application and Domain. Application depends on Domain abstractions and DTOs. Domain does not depend on outer layers.

## API Layer

Controllers live under `src/Languag.io.Api/Controllers`:

- `DecksController` manages deck CRUD and deck study data.
- `FeedController` returns the authenticated social/study feed.
- `FriendsController` manages friend requests, friendships, and friendship status.
- `NotificationsController` manages notifications and unread counts.
- `UsersController` manages current user profile, public profile lookup, username checks, and profile-picture upload lifecycle.
- `UsersWebhookController` receives Kinde user webhooks.

Authentication is configured in `Program.cs` using JWT bearer tokens from Kinde. Controllers use claims from the authenticated principal to resolve the current user.

## Application Layer

The application layer is organized by feature:

```text
ActivityLogs/
Decks/
Feed/
Friends/
Notifications/
StudySessions/
Users/
```

Feature services coordinate use cases and depend on repository interfaces. For example:

- `UserProfileService` handles profile reads and profile updates.
- `ProfilePictureService` owns the presigned upload and completion workflow.
- `FriendService` owns friend request and friendship mutations.
- `FeedService` retrieves feed data through `IFeedRepository`.

DTOs returned from the application layer are shaped for API consumers, not for database storage.

## Domain Model

Core entities include:

- `User`
- `Deck`
- `Card`
- `StudySession`
- `StudySessionResponse`
- `ActivityLog`
- `FriendRequest`
- `Friendship`
- `Notification`

`User.ProfilePictureObjectKey` stores the S3 object key for a user's custom profile picture. Public URLs are built at read time through the configured CloudFront base URL.

## Persistence

`AppDbContext` lives in `src/Languag.io.Infrastructure/Persistence`. EF Core migrations live under `src/Languag.io.Infrastructure/Migrations`.

Repositories use EF Core projections to keep API responses small and avoid leaking entity graphs into higher layers. Timeline-style endpoints use cursor pagination through shared application types such as `CursorPage<T>` and `TimelineCursor`.

The API currently runs pending migrations on startup. That keeps Railway deploys simple, but for larger production usage this should eventually move to an explicit migration job.

## Profile Pictures, S3, And CloudFront

The profile-picture feature splits responsibility deliberately:

- Frontend compresses images to `256x256` WebP.
- API validates the requested upload size and content type.
- API signs a short-lived S3 POST policy.
- Browser uploads directly to S3.
- API validates the uploaded S3 object before saving the key.
- PostgreSQL stores only the object key.
- API DTOs expose CloudFront URLs derived from the object key.

Key backend types:

- `ProfilePictureService` in `Application/Users`
- `IProfilePictureStorage` and `IProfilePictureUrlBuilder` in `Application/Users/ProfilePictureUpload.cs`
- `S3ProfilePictureStorage` in `Infrastructure/Storage`
- `UserProfileRepository`, `FeedRepository`, and `FriendshipRepository` for URL projection in profile/social reads

Object keys are scoped by user ID:

```text
profile-pictures/{userId:N}/{imageId:N}.webp
```

Upload abuse controls include:

- exact `image/webp` content-type checks,
- maximum upload size validation,
- short presigned upload TTL,
- object ownership checks,
- S3 metadata validation on completion,
- cleanup of invalid and replaced objects,
- upload endpoint rate limiting.

## Auth And Identity

Kinde owns user authentication. The backend validates Kinde JWT access tokens with the configured authority and audience.

Kinde webhooks synchronize user identity into the local `Users` table. Product data references local user IDs, while external identity linkage is preserved through the user's Kinde external ID.

## Social Feed And Avatars

Feed, friends, notifications, and public profile views project user profile data into DTOs. After custom profile pictures were added, the API exposes CloudFront avatar URLs in the social reads that need them:

- profile DTOs,
- feed activity DTOs,
- suggested people DTOs,
- friend summary DTOs,
- friend request and notification DTOs when available.

The frontend still keeps initials and avatar color as a fallback, so broken or missing CDN images do not block rendering.

## Deployment Boundaries

Railway hosts the API and PostgreSQL. The frontend can be hosted separately, such as on Vercel. AWS owns image storage and delivery.

```text
Frontend host
  -> Railway API
  -> Railway PostgreSQL
  -> AWS S3
  -> AWS CloudFront
```

The frontend must configure its API URL and CloudFront base URL. The backend must configure database, Kinde, AWS, and CloudFront settings.

## Adding A Feature

1. Add or update domain entities if durable state changes.
2. Add application DTOs, commands, queries, services, and repository contracts.
3. Implement persistence or external integration in Infrastructure.
4. Expose the use case through an API controller and request/response contract.
5. Add or update tests.
6. Document new environment variables or operational requirements.
