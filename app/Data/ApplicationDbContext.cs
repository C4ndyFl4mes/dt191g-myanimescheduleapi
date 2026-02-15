using App.Enums;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NodaTime;

namespace App.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel, IdentityRole<int>, int>
{
    public DbSet<IndexedAnimeModel> IndexedAnimes { get; set; }
    public DbSet<ScheduleEntryModel> ScheduleEntries { get; set; }

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Konfigurera IndexedAnimeModel.
        var indexedAnimeEntity = modelBuilder.Entity<IndexedAnimeModel>();

        // Konvertera EStatus enum till string i databasen.
        indexedAnimeEntity
            .Property(ia => ia.Status)
            .HasConversion<string>()
            .HasMaxLength(20);
    
        // Konvertera Instant till long (Unix time ticks) i databasen.
        indexedAnimeEntity
            .Property(ia => ia.ReleaseInstant)
            .HasConversion(
                v => v.ToUnixTimeTicks(),
                v => Instant.FromUnixTimeTicks(v)
            );

        // Konfigurera ScheduleEntryModel.
        var scheduleEntryEntity = modelBuilder.Entity<ScheduleEntryModel>();

        // Sätt en sammansatt primärnyckel för ScheduleEntryModel baserat på UserId och IndexedAnimeId.
        scheduleEntryEntity.HasKey(se => new { se.UserId, se.IndexedAnimeId });

        // Konfigurera relationen mellan ScheduleEntryModel och UserModel.
        scheduleEntryEntity
            .HasOne(se => se.User)
            .WithMany()
            .HasForeignKey(se => se.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konfigurera relationen mellan ScheduleEntryModel och IndexedAnimeModel.
        scheduleEntryEntity
            .HasOne(se => se.IndexedAnime)
            .WithMany()
            .HasForeignKey(se => se.IndexedAnimeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konvertera EWeekday enum till string i databasen.
        scheduleEntryEntity
            .Property(se => se.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20);

        // Konvertera LocalTime till long (ticks since midnight) i databasen, hantera null-värden.
        scheduleEntryEntity
            .Property(se => se.LocalTime)
            .HasConversion(
                v => v.HasValue ? v.Value.TickOfDay : (long?)null,
                v => v.HasValue ? LocalTime.FromTicksSinceMidnight(v.Value) : null
            );
    }
}