using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace App.Data;

public class ApplicationDbContext : IdentityDbContext<UserModel, IdentityRole<int>, int>
{
    public DbSet<IndexedAnimeModel> IndexedAnimes { get; set; }
    public DbSet<ScheduleEntryModel> ScheduleEntries { get; set; }
    public DbSet<PostModel> Posts { get; set; }

    private static readonly LocalTimePattern ScheduleTimePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");

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
    
        // Konvertera Instant till UTC+0 DateTime när det lagras i databasen.
        indexedAnimeEntity
            .Property(ia => ia.ReleaseInstant)
            .HasConversion(
                v => v.ToDateTimeUtc(),
                v => Instant.FromDateTimeUtc(DateTime.SpecifyKind(v, DateTimeKind.Utc))
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
            
       // Konvertera LocalTime till en text sträng char 5.
        scheduleEntryEntity
            .Property(se => se.LocalTime)
            .HasConversion(
                v => ScheduleTimePattern.Format(v),
                v => ScheduleTimePattern.Parse(v).Value
            )
            .HasMaxLength(5)
            .HasColumnType("char(5)");

        // Konfigurera PostModel.
        var postEntity = modelBuilder.Entity<PostModel>();

        // Konfigurera relationen mellan PostModel och UserModel.
        postEntity
            .HasOne(pe => pe.Author)
            .WithMany()
            .HasForeignKey(pe => pe.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Konfigurera relationen mellan PostModel och IndexedAnimeModel.
        postEntity
            .HasOne(pe => pe.Anime)
            .WithMany()
            .HasForeignKey(pe => pe.AnimeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Konvertera Instant till DateTime UTC+0 när det lagras i databasen.
        postEntity
            .Property(pe => pe.CreatedAt)
            .HasConversion(
                v => v.ToDateTimeUtc(),
                v => Instant.FromDateTimeUtc(DateTime.SpecifyKind(v, DateTimeKind.Utc))
            );
    }
}