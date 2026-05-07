using Languag.io.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Languag.io.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Deck> Decks => Set<Deck>();
    public DbSet<DeckVersion> DeckVersions => Set<DeckVersion>();
    public DbSet<DeckVersionCard> DeckVersionCards => Set<DeckVersionCard>();
    public DbSet<DeckVersionCardChoice> DeckVersionCardChoices => Set<DeckVersionCardChoice>();
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<CardChoice> CardChoices => Set<CardChoice>();
    public DbSet<AudioAsset> AudioAssets => Set<AudioAsset>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudySessionResponse> StudySessionResponses => Set<StudySessionResponse>();
    public DbSet<CardReviewState> CardReviewStates => Set<CardReviewState>();
    public DbSet<Saga> Sagas => Set<Saga>();
    public DbSet<SagaChapter> SagaChapters => Set<SagaChapter>();
    public DbSet<SagaLesson> SagaLessons => Set<SagaLesson>();
    public DbSet<SagaProgress> SagaProgresses => Set<SagaProgress>();
    public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
    public DbSet<Friendship> Friendships => Set<Friendship>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AiDeckGenerationJob> AiDeckGenerationJobs => Set<AiDeckGenerationJob>();
    public DbSet<AiSagaGenerationJob> AiSagaGenerationJobs => Set<AiSagaGenerationJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<Deck>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Title).IsRequired().HasMaxLength(200);
            builder.Property(d => d.Description).HasMaxLength(1000);
            builder.Property(d => d.Category).HasMaxLength(80);
            builder.Property(d => d.Color).HasMaxLength(20);
            builder.Property(d => d.CurrentVersionNumber).IsRequired().HasDefaultValue(1);

            builder.HasMany(d => d.Cards)
                   .WithOne(c => c.Deck!)
                   .HasForeignKey(c => c.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(d => d.Versions)
                   .WithOne(v => v.Deck)
                   .HasForeignKey(v => v.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                   .WithMany(u => u.Decks)
                   .HasForeignKey(u => u.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired(false);
        });

        modelBuilder.Entity<DeckVersion>(builder =>
        {
            builder.HasKey(v => v.Id);
            builder.Property(v => v.VersionNumber).IsRequired();
            builder.Property(v => v.Title).IsRequired().HasMaxLength(200);
            builder.Property(v => v.Description).HasMaxLength(1000);
            builder.Property(v => v.Category).HasMaxLength(80);
            builder.Property(v => v.Color).HasMaxLength(20);
            builder.Property(v => v.Visibility).IsRequired();
            builder.Property(v => v.CreatedAtUtc).IsRequired();

            builder.HasOne(v => v.CreatedByUser)
                   .WithMany()
                   .HasForeignKey(v => v.CreatedByUserId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasMany(v => v.Cards)
                   .WithOne(c => c.DeckVersion)
                   .HasForeignKey(c => c.DeckVersionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(v => new { v.DeckId, v.VersionNumber }).IsUnique();
            builder.HasIndex(v => v.CreatedByUserId);
        });

        modelBuilder.Entity<DeckVersionCard>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Type).IsRequired().HasMaxLength(40);
            builder.Property(c => c.FrontText).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.BackText).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.ExampleSentence).HasMaxLength(2000);

            builder.HasOne(c => c.FrontAudioAsset)
                   .WithMany()
                   .HasForeignKey(c => c.FrontAudioAssetId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasMany(c => c.Choices)
                   .WithOne(choice => choice.DeckVersionCard)
                   .HasForeignKey(choice => choice.DeckVersionCardId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => new { c.DeckVersionId, c.Order });
            builder.HasIndex(c => c.OriginalCardId);
            builder.HasIndex(c => c.FrontAudioAssetId);
        });

        modelBuilder.Entity<DeckVersionCardChoice>(builder =>
        {
            builder.HasKey(choice => choice.Id);
            builder.Property(choice => choice.Text).IsRequired().HasMaxLength(1000);
            builder.HasIndex(choice => new { choice.DeckVersionCardId, choice.Order });
            builder.HasIndex(choice => choice.OriginalChoiceId);
        });

        modelBuilder.Entity<Card>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Type).IsRequired().HasMaxLength(40).HasDefaultValue(CardTypes.Flashcard);
            builder.Property(c => c.FrontText).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.BackText).IsRequired().HasMaxLength(1000);
            builder.Property(c => c.ExampleSentence).HasMaxLength(2000);

            builder.HasOne(c => c.FrontAudioAsset)
                   .WithMany()
                   .HasForeignKey(c => c.FrontAudioAssetId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasMany(c => c.Choices)
                   .WithOne(choice => choice.Card!)
                   .HasForeignKey(choice => choice.CardId)
                   .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CardChoice>(builder =>
        {
            builder.HasKey(choice => choice.Id);
            builder.Property(choice => choice.Text).IsRequired().HasMaxLength(1000);
            builder.HasIndex(choice => new { choice.CardId, choice.Order });
        });

        modelBuilder.Entity<AudioAsset>(builder =>
        {
            builder.HasKey(asset => asset.Id);
            builder.Property(asset => asset.TextHash).IsRequired().HasMaxLength(64);
            builder.Property(asset => asset.NormalizedText).IsRequired().HasMaxLength(1000);
            builder.Property(asset => asset.LanguageCode).IsRequired().HasMaxLength(20);
            builder.Property(asset => asset.Provider).IsRequired().HasMaxLength(40);
            builder.Property(asset => asset.Model).IsRequired().HasMaxLength(80);
            builder.Property(asset => asset.Voice).IsRequired().HasMaxLength(40);
            builder.Property(asset => asset.Speed).HasPrecision(4, 2);
            builder.Property(asset => asset.InstructionsHash).IsRequired().HasMaxLength(64);
            builder.Property(asset => asset.StorageKey).IsRequired().HasMaxLength(512);
            builder.Property(asset => asset.PublicUrl).IsRequired().HasMaxLength(1000);
            builder.Property(asset => asset.Status).IsRequired();
            builder.Property(asset => asset.CreatedAtUtc).IsRequired();
            builder.Property(asset => asset.UpdatedAtUtc).IsRequired();
            builder.HasIndex(asset => asset.TextHash).IsUnique();
        });

        modelBuilder.Entity<User>(builder => {
            builder.HasKey(u => u.Id);
            builder.Property(u => u.ExternalId).IsRequired().HasMaxLength(255);
            builder.Property(u => u.Name).HasMaxLength(100);
            builder.Property(u => u.Email).HasMaxLength(255);
            builder.Property(u => u.Username).HasMaxLength(50);
            builder.Property(u => u.CreatedAtUtc).IsRequired().HasDefaultValueSql("NOW()");
            builder.Property(u => u.UpdatedAtUtc).IsRequired().HasDefaultValueSql("NOW()");
            builder.Property(u => u.HasBeenOnboarded).HasDefaultValue(false);
            builder.Property(u => u.DailyCardsGoal).HasDefaultValue(0);
            builder.Property(u => u.AvatarColor).HasMaxLength(20).HasDefaultValue("teal");
            builder.Property(u => u.ProfilePictureObjectKey).HasMaxLength(512);
            builder.Property(u => u.ProfileDescription).HasMaxLength(280).HasDefaultValue(string.Empty);
            builder.Property(u => u.About).HasMaxLength(2000).HasDefaultValue(string.Empty);
            builder.Property(u => u.IsPublicProfile).HasDefaultValue(false);
            builder.HasIndex(u => u.ExternalId).IsUnique();
            builder.HasIndex(u => u.Username)
                .IsUnique()
                .HasFilter("\"Username\" IS NOT NULL AND \"Username\" <> ''");
        });

        modelBuilder.Entity<ActivityLog>(builder =>
        {
            builder.HasKey(a => a.Id);
            builder.Property(a => a.Type).IsRequired();
            builder.Property(a => a.OccurredAtUtc).IsRequired();
            builder.Property(a => a.CreatedAtUtc).IsRequired();
            builder.Property(a => a.Metadata).HasMaxLength(2000);

            builder.HasOne(a => a.User)
                   .WithMany(u => u.ActivityLogs)
                   .HasForeignKey(a => a.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(a => a.Deck)
                   .WithMany(d => d.ActivityLogs)
                   .HasForeignKey(a => a.DeckId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasIndex(a => new { a.UserId, a.OccurredAtUtc });
            builder.HasIndex(a => new { a.UserId, a.Type, a.OccurredAtUtc });
        });

        modelBuilder.Entity<StudySession>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.CreatedAtUtc).IsRequired();
            builder.Property(s => s.PercentageCorrect).HasPrecision(5, 2);

            builder.HasOne(s => s.Deck)
                   .WithMany(d => d.StudySessions)
                   .HasForeignKey(s => s.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.DeckVersion)
                   .WithMany(v => v.StudySessions)
                   .HasForeignKey(s => s.DeckVersionId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(s => s.User)
                   .WithMany(u => u.StudySessions)
                   .HasForeignKey(s => s.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Responses)
                   .WithOne(r => r.StudySession)
                   .HasForeignKey(r => r.StudySessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.UserId, s.CreatedAtUtc });
            builder.HasIndex(s => new { s.DeckId, s.CreatedAtUtc });
            builder.HasIndex(s => s.DeckVersionId);
        });

        modelBuilder.Entity<StudySessionResponse>(builder =>
        {
            builder.HasKey(r => r.Id);
            builder.Property(r => r.WasCorrect).IsRequired();

            builder.HasOne(r => r.StudySession)
                   .WithMany(s => s.Responses)
                   .HasForeignKey(r => r.StudySessionId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Deck)
                   .WithMany(d => d.StudySessionResponses)
                   .HasForeignKey(r => r.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Card)
                   .WithMany(c => c.StudySessionResponses)
                   .HasForeignKey(r => r.CardId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(r => r.DeckVersionCard)
                   .WithMany(c => c.StudySessionResponses)
                   .HasForeignKey(r => r.DeckVersionCardId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(r => r.User)
                   .WithMany(u => u.StudySessionResponses)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.StudySessionId, r.CardId });
            builder.HasIndex(r => new { r.UserId, r.DeckId });
            builder.HasIndex(r => r.DeckVersionCardId);
        });

        modelBuilder.Entity<CardReviewState>(builder =>
        {
            builder.HasKey(r => new { r.UserId, r.CardId });
            builder.Property(r => r.DueAtUtc).IsRequired();
            builder.Property(r => r.EaseFactor).HasPrecision(4, 2);
            builder.Property(r => r.IntervalDays).IsRequired();
            builder.Property(r => r.RepetitionCount).IsRequired();
            builder.Property(r => r.LapseCount).IsRequired();
            builder.Property(r => r.TotalReviews).IsRequired();
            builder.Property(r => r.CorrectReviews).IsRequired();

            builder.HasOne(r => r.User)
                   .WithMany(u => u.CardReviewStates)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Deck)
                   .WithMany(d => d.CardReviewStates)
                   .HasForeignKey(r => r.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.Card)
                   .WithMany(c => c.ReviewStates)
                   .HasForeignKey(r => r.CardId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.UserId, r.DeckId, r.DueAtUtc });
            builder.HasIndex(r => new { r.UserId, r.DueAtUtc });
        });

        modelBuilder.Entity<AiDeckGenerationJob>(builder =>
        {
            builder.HasKey(job => job.Id);
            builder.Property(job => job.Prompt).IsRequired().HasMaxLength(1000);
            builder.Property(job => job.TargetLanguage).HasMaxLength(80);
            builder.Property(job => job.NativeLanguage).HasMaxLength(80);
            builder.Property(job => job.Difficulty).IsRequired().HasMaxLength(40);
            builder.Property(job => job.ErrorMessage).HasMaxLength(2000);
            builder.Property(job => job.Status).IsRequired();
            builder.Property(job => job.AudioStatus)
                .IsRequired()
                .HasDefaultValue(AiDeckAudioStatus.NotRequested);
            builder.Property(job => job.IncludeAudio).HasDefaultValue(false);
            builder.Property(job => job.RequestedMultiChoiceCount).HasDefaultValue(0);
            builder.Property(job => job.CreatedAtUtc).IsRequired();
            builder.Property(job => job.RetryCount).HasDefaultValue(0);

            builder.HasOne(job => job.User)
                   .WithMany(user => user.AiDeckGenerationJobs)
                   .HasForeignKey(job => job.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(job => job.CreatedDeck)
                   .WithMany()
                   .HasForeignKey(job => job.CreatedDeckId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasIndex(job => new { job.Status, job.CreatedAtUtc });
            builder.HasIndex(job => new { job.UserId, job.CreatedAtUtc });
        });

        modelBuilder.Entity<AiSagaGenerationJob>(builder =>
        {
            builder.HasKey(job => job.Id);
            builder.Property(job => job.Prompt).IsRequired().HasMaxLength(1000);
            builder.Property(job => job.TargetLanguage).HasMaxLength(80);
            builder.Property(job => job.NativeLanguage).HasMaxLength(80);
            builder.Property(job => job.Difficulty).IsRequired().HasMaxLength(40);
            builder.Property(job => job.ErrorMessage).HasMaxLength(2000);
            builder.Property(job => job.Status).IsRequired();
            builder.Property(job => job.AudioStatus)
                .IsRequired()
                .HasDefaultValue(AiSagaAudioStatus.NotRequested);
            builder.Property(job => job.IncludeAudio).HasDefaultValue(false);
            builder.Property(job => job.RequestedMultiChoiceCountPerDeck).HasDefaultValue(0);
            builder.Property(job => job.UsageWeekStartUtc).IsRequired();
            builder.Property(job => job.CreatedAtUtc).IsRequired();
            builder.Property(job => job.RetryCount).HasDefaultValue(0);

            builder.HasOne(job => job.User)
                   .WithMany(user => user.AiSagaGenerationJobs)
                   .HasForeignKey(job => job.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(job => job.CreatedSaga)
                   .WithMany()
                   .HasForeignKey(job => job.CreatedSagaId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasIndex(job => new { job.Status, job.CreatedAtUtc });
            builder.HasIndex(job => new { job.UserId, job.CreatedAtUtc });
            builder.HasIndex(job => new { job.UserId, job.UsageWeekStartUtc })
                .IsUnique();
        });

        modelBuilder.Entity<Saga>(builder =>
        {
            builder.HasKey(s => s.Id);
            builder.Property(s => s.Title).IsRequired().HasMaxLength(200);
            builder.Property(s => s.Description).HasMaxLength(1000);
            builder.Property(s => s.Category).HasMaxLength(80);
            builder.Property(s => s.Color).HasMaxLength(20);
            builder.Property(s => s.CreatedAtUtc).IsRequired();
            builder.Property(s => s.UpdatedAtUtc).IsRequired();

            builder.HasOne(s => s.User)
                   .WithMany(u => u.Sagas)
                   .HasForeignKey(s => s.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Chapters)
                   .WithOne(c => c.Saga)
                   .HasForeignKey(c => c.SagaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(s => s.Progresses)
                   .WithOne(p => p.Saga)
                   .HasForeignKey(p => p.SagaId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(s => new { s.OwnerId, s.UpdatedAtUtc });
            builder.HasIndex(s => s.Visibility);
        });

        modelBuilder.Entity<SagaChapter>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.Title).IsRequired().HasMaxLength(200);
            builder.Property(c => c.Description).HasMaxLength(1000);

            builder.HasMany(c => c.Lessons)
                   .WithOne(l => l.SagaChapter)
                   .HasForeignKey(l => l.SagaChapterId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(c => new { c.SagaId, c.Order });
        });

        modelBuilder.Entity<SagaLesson>(builder =>
        {
            builder.HasKey(l => l.Id);
            builder.Property(l => l.Title).HasMaxLength(200);
            builder.Property(l => l.Description).HasMaxLength(1000);

            builder.HasOne(l => l.Deck)
                   .WithMany(d => d.SagaLessons)
                   .HasForeignKey(l => l.DeckId)
                   .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(l => new { l.SagaChapterId, l.Order });
            builder.HasIndex(l => l.DeckId);
        });

        modelBuilder.Entity<SagaProgress>(builder =>
        {
            builder.HasKey(p => new { p.SagaId, p.UserId });
            builder.Property(p => p.StartedAtUtc).IsRequired();
            builder.Property(p => p.LastStudiedAtUtc).IsRequired();

            builder.HasOne(p => p.User)
                   .WithMany(u => u.SagaProgresses)
                   .HasForeignKey(p => p.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(p => p.LastStudiedLesson)
                   .WithMany()
                   .HasForeignKey(p => p.LastStudiedLessonId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasOne(p => p.HighestCompletedLesson)
                   .WithMany()
                   .HasForeignKey(p => p.HighestCompletedLessonId)
                   .OnDelete(DeleteBehavior.SetNull)
                   .IsRequired(false);

            builder.HasIndex(p => new { p.UserId, p.LastStudiedAtUtc });
        });
    }
}
