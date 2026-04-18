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
    public DbSet<Card> Cards => Set<Card>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<StudySession> StudySessions => Set<StudySession>();
    public DbSet<StudySessionResponse> StudySessionResponses => Set<StudySessionResponse>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Deck>(builder =>
        {
            builder.HasKey(d => d.Id);
            builder.Property(d => d.Title).IsRequired().HasMaxLength(200);

            builder.HasMany(d => d.Cards)
                   .WithOne(c => c.Deck!)
                   .HasForeignKey(c => c.DeckId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(d => d.User)
                   .WithMany(u => u.Decks)
                   .HasForeignKey(u => u.OwnerId)
                   .OnDelete(DeleteBehavior.Cascade)
                   .IsRequired(false);
        });

        modelBuilder.Entity<Card>(builder =>
        {
            builder.HasKey(c => c.Id);
            builder.Property(c => c.FrontText).IsRequired();
            builder.Property(c => c.BackText).IsRequired();
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
            builder.Property(u => u.ProfileDescription).HasDefaultValue(string.Empty);
            builder.Property(u => u.About).HasDefaultValue(string.Empty);
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
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(r => r.User)
                   .WithMany(u => u.StudySessionResponses)
                   .HasForeignKey(r => r.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(r => new { r.StudySessionId, r.CardId });
            builder.HasIndex(r => new { r.UserId, r.DeckId });
        });
    }
}
