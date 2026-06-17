using Microsoft.EntityFrameworkCore;
using BattleTanks_Backend.Domain.Entities;

namespace BattleTanks_Backend.Infrastructure.Data;

public class BattleTanksDbContext : DbContext
{
    public BattleTanksDbContext(DbContextOptions<BattleTanksDbContext> options)
        : base(options)
    {
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

            entity.Property(p => p.FirebaseUid)
                .HasMaxLength(128);

            entity.Property(p => p.Username)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.Email)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasIndex(p => p.FirebaseUid).IsUnique();
            entity.HasIndex(p => p.Username).IsUnique();
            entity.HasIndex(p => p.Email).IsUnique();

            entity.HasIndex(p => p.TotalScore);

            entity.Property(p => p.GamesPlayed).HasDefaultValue(0);
            entity.Property(p => p.Wins).HasDefaultValue(0);
            entity.Property(p => p.TotalScore).HasDefaultValue(0);
            entity.Property(p => p.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<GameSession>(entity =>
        {
            entity.HasKey(gs => gs.Id);

            entity.Property(gs => gs.Name)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(gs => gs.SelectedMap)
                .HasMaxLength(50)
                .HasDefaultValue("default");

            entity.Property(gs => gs.MaxPlayers).HasDefaultValue(4);
            entity.Property(gs => gs.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(gs => gs.Status)
                .HasConversion<string>()
                .HasMaxLength(20);

            entity.Property(gs => gs.Region)
                .HasMaxLength(10)
                .HasDefaultValue("LATAM");

            entity.HasIndex(gs => gs.Status);
            entity.HasIndex(gs => gs.CreatedAt);
            entity.HasIndex(gs => gs.Region);

            // Índice compuesto para consultas de partidas por región y fecha
            entity.HasIndex(gs => new { gs.Region, gs.CreatedAt });
        });

        modelBuilder.Entity<Score>(entity =>
        {
            entity.HasKey(s => s.Id);

            entity.Property(s => s.Points).IsRequired();
            entity.Property(s => s.AchievedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(s => s.Player)
                .WithMany(p => p.Scores)
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Session)
                .WithMany(gs => gs.Scores)
                .HasForeignKey(s => s.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.PlayerId);
            entity.HasIndex(s => s.SessionId);
            entity.HasIndex(s => s.Points);

            entity.HasIndex(s => new { s.PlayerId, s.AchievedAt });
            entity.HasIndex(s => new { s.SessionId, s.Points });
        });

        modelBuilder.Entity<RoomPlayer>(entity =>
        {
            entity.HasKey(rp => rp.Id);

            entity.Property(rp => rp.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(rp => rp.IsActive).HasDefaultValue(true);

            entity.HasOne(rp => rp.Player)
                .WithMany()
                .HasForeignKey(rp => rp.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Session)
                .WithMany()
                .HasForeignKey(rp => rp.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(rp => rp.SessionId);
            entity.HasIndex(rp => rp.PlayerId);
            entity.HasIndex(rp => new { rp.SessionId, rp.PlayerId }).IsUnique();

            entity.HasIndex(rp => new { rp.SessionId, rp.IsActive });
        });

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(cm => cm.Id);

            entity.Property(cm => cm.PlayerName)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(cm => cm.Message)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(cm => cm.Timestamp).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(cm => cm.Room)
                .WithMany()
                .HasForeignKey(cm => cm.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cm => cm.Player)
                .WithMany()
                .HasForeignKey(cm => cm.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cm => cm.RoomId);
            entity.HasIndex(cm => cm.Timestamp);

            entity.HasIndex(cm => new { cm.RoomId, cm.Timestamp });
            entity.HasIndex(cm => cm.PlayerId);
        });
    }
}

