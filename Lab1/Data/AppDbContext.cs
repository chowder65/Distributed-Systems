using Microsoft.EntityFrameworkCore;
using RetroGameExchange.Models;

namespace RetroGameExchange.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Game> Games => Set<Game>();
    public DbSet<TradeOffer> TradeOffers => Set<TradeOffer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Name).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(320).IsRequired();
            entity.Property(u => u.PasswordHash).IsRequired();
            entity.Property(u => u.StreetAddress).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<Game>(entity =>
        {
            entity.Property(g => g.Name).HasMaxLength(300).IsRequired();
            entity.Property(g => g.Publisher).HasMaxLength(200).IsRequired();
            entity.Property(g => g.GamingSystem).HasMaxLength(100).IsRequired();
            entity.Property(g => g.Condition).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(g => g.Owner)
                .WithMany(u => u.Games)
                .HasForeignKey(g => g.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<TradeOffer>(entity =>
        {
            entity.Property(t => t.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasOne(t => t.OfferedGame)
                .WithMany()
                .HasForeignKey(t => t.OfferedGameId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.RequestedGame)
                .WithMany()
                .HasForeignKey(t => t.RequestedGameId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(t => t.OfferingUser)
                .WithMany()
                .HasForeignKey(t => t.OfferingUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
