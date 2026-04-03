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
            builder.HasIndex(u => u.ExternalId).IsUnique();
        });
    }
}

