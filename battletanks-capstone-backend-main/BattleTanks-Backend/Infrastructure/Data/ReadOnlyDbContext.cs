using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Infrastructure.Data;

public class ReadOnlyDbContext : DbContext
{
    public ReadOnlyDbContext(DbContextOptions<ReadOnlyDbContext> options)
        : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public DbSet<Player> Players => Set<Player>();
    public DbSet<GameSession> GameSessions => Set<GameSession>();
    public DbSet<Score> Scores => Set<Score>();
    public DbSet<RoomPlayer> RoomPlayers => Set<RoomPlayer>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(p => p.Id);
            entity.Property(p => p.FirebaseUid).HasMaxLength(128);
            entity.Property(p => p.Username).IsRequired().HasMaxLength(50);
            entity.Property(p => p.Email).IsRequired().HasMaxLength(100);
            entity.HasIndex(p => p.FirebaseUid).IsUnique();
            entity.HasIndex(p => p.Username).IsUnique();
            entity.HasIndex(p => p.Email).IsUnique();
            entity.HasIndex(p => p.TotalScore);
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(gs => gs.Id);
            entity.Property(gs => gs.Name).IsRequired().HasMaxLength(100);
            entity.Property(gs => gs.SelectedMap).HasMaxLength(50).HasDefaultValue("default");
            entity.Property(gs => gs.Region).HasMaxLength(10).HasDefaultValue("LATAM");
            entity.Property(gs => gs.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(gs => gs.Status);
            entity.HasIndex(gs => gs.Region);
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.Property(s => s.Points).IsRequired();
            entity.HasOne(s => s.Player).WithMany(p => p.Scores).HasForeignKey(s => s.PlayerId);
            entity.HasOne(s => s.Session).WithMany(gs => gs.Scores).HasForeignKey(s => s.SessionId);
            entity.HasIndex(s => s.Points);
        });

        modelBuilder.Entity<RoomPlayer>(entity =>
        {
            entity.HasKey(rp => rp.Id);
            entity.HasIndex(rp => rp.SessionId);
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.Id);
            entity.HasIndex(cm => cm.RoomId);
        });
    }
}
