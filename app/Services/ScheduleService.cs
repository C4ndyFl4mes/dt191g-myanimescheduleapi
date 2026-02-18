using App.Data;
using App.DTOs;
using App.Enums;
using App.Exceptions;
using App.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace App.Services;

public class ScheduleService(ApplicationDbContext _context)
{

    private static readonly LocalTimePattern ScheduleTimePattern = LocalTimePattern.CreateWithInvariantCulture("HH:mm");

    // Metod för att hämta en användares schema baserat på deras schedule entries.
    public async Task<ScheduleResponse> GetScheduleByUserID(int userID)
    {
        UserModel? user = await _context.Users.FindAsync(userID)
            ?? throw new NotFoundException("User not found.");

        List<ScheduleEntryModel> scheduleEntries = await _context.ScheduleEntries
            .Where(se => se.UserId == user.Id)
            .Include(se => se.IndexedAnime)
            .ToListAsync();

        Instant now = SystemClock.Instance.GetCurrentInstant();
        DateTimeZone userZone = DateTimeZoneProviders.Tzdb[user.TimeZoneID];
        ZonedDateTime zoneNow = now.InZone(userZone);

        // Denna veckas måndag.
        int mondayOffset = ((int)zoneNow.DayOfWeek - (int)IsoDayOfWeek.Monday + 7) % 7;
        LocalDate currentMonday = zoneNow.Date.PlusDays(-mondayOffset);
        LocalDate currentSunday = currentMonday.PlusDays(6);

        Dictionary<EWeekday, List<ScheduleEntryResponse>> weekDaysDictionary = new();

        foreach (ScheduleEntryModel entry in scheduleEntries)
        {
            // Status får inte vara FinishedAiring.
            if (entry.IndexedAnime!.Status == EStatus.FinishedAiring)
            {
                continue; // Om den är det skippas den.
            }

            // Veckodag och tid som ska anges för entryn.
            EWeekday displayDay = entry.DayOfWeek;
            LocalTime displayTime = entry.LocalTime;

            LocalDate displayDate = currentMonday.PlusDays((int)displayDay);
            LocalDateTime displayDateTime = displayDate + displayTime;
            Instant displayInstant = userZone.AtStrictly(displayDateTime).ToInstant();

            // I schemat om Status är CurrentlyAiring eller om release instant förfaller i det förflutna.
            Instant releaseInstant = entry.IndexedAnime.ReleaseInstant;
            ZonedDateTime releaseInUserZone = releaseInstant.InZone(userZone);
            LocalDate releaseDate = releaseInUserZone.Date;

            bool isCurrentlyAiring = entry.IndexedAnime.Status == EStatus.CurrentlyAiring;
            bool hasAlreadyReleased = releaseInstant <= now;

            if (!isCurrentlyAiring && !hasAlreadyReleased)
            {
                continue;
            }

            // Lägger till i schemat.
            ScheduleEntryResponse scheduleEntryResponse = new()
            {
                Id = entry.IndexedAnimeId,
                Title = entry.IndexedAnime.Title,
                ImageURL = entry.IndexedAnime.ImageURL,
                Time = ScheduleTimePattern.Format(displayTime)
            };

            // Grupperar entries per veckodag.
            if (!weekDaysDictionary.ContainsKey(displayDay))
            {
                weekDaysDictionary[displayDay] = new List<ScheduleEntryResponse>();
            }
            weekDaysDictionary[displayDay].Add(scheduleEntryResponse);
        }

        List<ScheduleWeekDayResponse> scheduleWeekDays = weekDaysDictionary
            .OrderBy(kvp => kvp.Key)
            .Select(kvp => new ScheduleWeekDayResponse
            {
                DayOfWeek = kvp.Key,
                ScheduleEntries = kvp.Value.OrderBy(se => se.Time).ToList()
            })
            .ToList();

        return new ScheduleResponse
        {
            WeekDays = scheduleWeekDays
        };
    }

    // Metod för att lägga till en schedule entry för en användare.
    public async Task AddScheduleEntry(int userId, ScheduleRequest request)
    {
        ScheduleEntryModel? existingEntry = await _context.ScheduleEntries
            .FirstOrDefaultAsync(se => se.UserId == userId && se.IndexedAnime!.Mal_ID == request.Mal_ID);

        if (existingEntry != null) throw new ConflictException("Schedule entry already exists for this user and anime.");

        IndexedAnimeModel? indexedAnime = await _context.IndexedAnimes.FirstOrDefaultAsync(ia => ia.Mal_ID == request.Mal_ID)
        ?? throw new NotFoundException("Anime not found in index.");

        // Förhindrar att animes som redan har sänts inte längre kan läggas till i schemat.
        if (indexedAnime.Status == EStatus.FinishedAiring) throw new BadRequestException("Cannot add finished airing anime to schedule.");

        EWeekday? watchDay = request.WatchDay;
        TimeOnly? time = request.Time;

        if (watchDay == null || time == null)
        {
            UserModel? user = await _context.Users.FindAsync(userId)
            ?? throw new NotFoundException("User not found.");

            // Om användaren inte har angett dag och tid, beräknas det baserat på anime-releasen och användarens tidszon.
            DateTimeZone userZone = DateTimeZoneProviders.Tzdb[user.TimeZoneID];
            ZonedDateTime broadcastInUserZone = indexedAnime.ReleaseInstant.InZone(userZone);

            watchDay = broadcastInUserZone.DayOfWeek switch
            {
                IsoDayOfWeek.Monday => EWeekday.Monday,
                IsoDayOfWeek.Tuesday => EWeekday.Tuesday,
                IsoDayOfWeek.Wednesday => EWeekday.Wednesday,
                IsoDayOfWeek.Thursday => EWeekday.Thursday,
                IsoDayOfWeek.Friday => EWeekday.Friday,
                IsoDayOfWeek.Saturday => EWeekday.Saturday,
                IsoDayOfWeek.Sunday => EWeekday.Sunday,
                _ => throw new BadRequestException("Invalid day of week.")
            };
            LocalTime localTime = broadcastInUserZone.TimeOfDay;
            time = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(localTime.TickOfDay));
        }

        ScheduleEntryModel newEntry = new()
        {
            UserId = userId,
            DayOfWeek = (EWeekday)watchDay,
            LocalTime = LocalTime.FromTicksSinceMidnight(time!.Value.Ticks),
            IndexedAnime = await _context.IndexedAnimes.FirstOrDefaultAsync(ia => ia.Mal_ID == request.Mal_ID) ?? throw new NotFoundException("Anime not found in index.")
        };

        await _context.ScheduleEntries.AddAsync(newEntry);

        await _context.SaveChangesAsync();
    }

    // Metod för att uppdatera en schedule entry.
    public async Task UpdateScheduleEntry(int userId, ScheduleUpdateRequest request)
    {
        ScheduleEntryModel? entry = await _context.ScheduleEntries
            .FirstOrDefaultAsync(se => se.UserId == userId && se.IndexedAnimeId == request.Id)
            ?? throw new NotFoundException("Schedule entry doesn't exist for this user and anime.");

        IndexedAnimeModel? indexedAnime = await _context.IndexedAnimes.FirstOrDefaultAsync(ia => ia.Id == request.Id)
        ?? throw new NotFoundException("Anime not found in index.");

        // Förhindrar att animes som redan har sänts inte längre kan läggas till i schemat.
        if (indexedAnime.Status == EStatus.FinishedAiring) throw new BadRequestException("Cannot update finished airing anime to schedule.");

        EWeekday? watchDay = request.WatchDay;
        TimeOnly? time = request.Time;

        if (watchDay == null || time == null)
        {
            UserModel? user = await _context.Users.FindAsync(userId)
            ?? throw new Exception("User not found.");

            // Om användaren inte har angett dag och tid, beräknas det baserat på anime-releasen och användarens tidszon.
            DateTimeZone userZone = DateTimeZoneProviders.Tzdb[user.TimeZoneID];
            ZonedDateTime broadcastInUserZone = indexedAnime.ReleaseInstant.InZone(userZone);

            watchDay = broadcastInUserZone.DayOfWeek switch
            {
                IsoDayOfWeek.Monday => EWeekday.Monday,
                IsoDayOfWeek.Tuesday => EWeekday.Tuesday,
                IsoDayOfWeek.Wednesday => EWeekday.Wednesday,
                IsoDayOfWeek.Thursday => EWeekday.Thursday,
                IsoDayOfWeek.Friday => EWeekday.Friday,
                IsoDayOfWeek.Saturday => EWeekday.Saturday,
                IsoDayOfWeek.Sunday => EWeekday.Sunday,
                _ => throw new BadRequestException("Invalid day of week.")
            };
            LocalTime localTime = broadcastInUserZone.TimeOfDay;
            time = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(localTime.TickOfDay));
        }

        entry.LocalTime = LocalTime.FromTicksSinceMidnight(time.Value.Ticks);
        entry.DayOfWeek = (EWeekday)watchDay;

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }

    // Metod för att radera en schedule entry.
    public async Task DeleteScheduleEntry(int userId, int scheduleEntryId)
    {
        ScheduleEntryModel? entry = await _context.ScheduleEntries
            .FirstOrDefaultAsync(se => se.UserId == userId && se.IndexedAnimeId == scheduleEntryId)
            ?? throw new Exception("Schedule entry doesn't exist for this user and anime.");

        _context.ScheduleEntries.Remove(entry);

        if (_context.ChangeTracker.HasChanges())
        {
            await _context.SaveChangesAsync();
        }
    }
}