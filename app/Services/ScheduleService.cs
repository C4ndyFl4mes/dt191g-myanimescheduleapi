using App.Data;
using App.DTOs;
using App.Enums;
using App.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace App.Services;

public class ScheduleService
{
    private readonly ApplicationDbContext _context;

    public ScheduleService(ApplicationDbContext context, UserManager<Models.UserModel> userManager)
    {
        _context = context;
    }

    // Metod för att lägga till en schedule entry för en användare.
    public async Task AddScheduleEntry(int userId, ScheduleRequest request)
    {
        ScheduleEntryModel? existingEntry = await _context.ScheduleEntries
            .FirstOrDefaultAsync(se => se.UserId == userId && se.IndexedAnime!.Mal_ID == request.Mal_ID);

        if (existingEntry != null)        {
            throw new Exception("Schedule entry already exists for this user and anime.");
        }
        
        EWeekday? watchDay = request.WatchDay;
        TimeOnly? time = request.Time;

        if (watchDay == null || time == null)
        {
            IndexedAnimeModel? indexedAnime = await _context.IndexedAnimes.FirstOrDefaultAsync(ia => ia.Mal_ID == request.Mal_ID);
            if (indexedAnime == null)
            {
                throw new Exception("Anime not found in index.");
            }

            UserModel? user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

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
                _ => throw new Exception("Invalid day of week.")
            };
            LocalTime localTime = broadcastInUserZone.TimeOfDay;
            time = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(localTime.TickOfDay));
        }

        ScheduleEntryModel newEntry = new()
        {
            UserId = userId,
            DayOfWeek = watchDay,
            LocalTime = LocalTime.FromTicksSinceMidnight(time!.Value.Ticks),
            IndexedAnime = await _context.IndexedAnimes.FirstOrDefaultAsync(ia => ia.Mal_ID == request.Mal_ID) ?? throw new Exception("Anime not found in index.")
        };

        await _context.ScheduleEntries.AddAsync(newEntry);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine($"An error occurred while adding the schedule entry: {ex.Message}");
            throw;
        }
    }

    // Metod för att hämta en användares schema baserat på deras schedule entries.
    public async Task<ScheduleResponse> GetScheduleByUserID(int userId)
    {
        UserModel? user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            throw new Exception("User not found.");
        }

         List<ScheduleEntryModel> scheduleEntries = await _context.ScheduleEntries
            .Where(se => se.UserId == user.Id)
            .Include(se => se.IndexedAnime)
            .ToListAsync();

        Instant now = SystemClock.Instance.GetCurrentInstant();
        DateTimeZone zone = DateTimeZoneProviders.Tzdb[user.TimeZoneID];
        ZonedDateTime zoneNow = now.InZone(zone);

        Dictionary<EWeekday, List<ScheduleEntryResponse>> weekDaysDictionary = new Dictionary<EWeekday, List<ScheduleEntryResponse>>(); // Temporär dictionary för att gruppera entries per veckodag.

        foreach (ScheduleEntryModel entry in scheduleEntries)
        {
            Instant releaseInstant = entry.IndexedAnime!.ReleaseInstant;
            Duration timeSinceRelease = now - releaseInstant;
            int weeksSinceRelease = (int)(timeSinceRelease / Duration.FromHours(24 * 7));

            int availableEpisodes = weeksSinceRelease + 1; // Eftersom första avsnittet är tillgängligt vid release så läggs 1 till.

            if (availableEpisodes < 0)
            {
                availableEpisodes = 0; // Om releasen är i framtiden, sätt till 0.
            }

            if (entry.IndexedAnime.TotalEpisodes != null)
            {
                availableEpisodes = Math.Min(availableEpisodes, entry.IndexedAnime.TotalEpisodes.Value);
            }

            int daysToAdd = ((int)entry.DayOfWeek! - (int)zoneNow.DayOfWeek + 7) % 7;

            if (daysToAdd == 0 && zoneNow.TimeOfDay > entry.LocalTime!.Value)
            {
                daysToAdd = 7; // Om det är samma dag men tiden har passerat, schemalägg till nästa vecka.
            }

            LocalDate nextDate = zoneNow.Date.PlusDays(daysToAdd);
            LocalDateTime nextLocalDateTime = nextDate + entry.LocalTime!.Value;
            Instant nextWatchInstant = zone.AtStrictly(nextLocalDateTime).ToInstant();

            // Om nästa schemalagda tittar-tillfälle är i framtiden, inkludera det i schemat.
            ScheduleEntryResponse scheduleEntryResponse = new()
            {
                Id = entry.IndexedAnimeId,
                Title = entry.IndexedAnime.Title,
                ImageURL = entry.IndexedAnime.ImageURL,
                Time = TimeSpan.FromTicks(entry.LocalTime!.Value.TickOfDay)
            };

            // Gruppera schemat per veckodag i dictionaryn.
            if (!weekDaysDictionary.ContainsKey(entry.DayOfWeek.Value))
            {
                weekDaysDictionary[entry.DayOfWeek.Value] = new List<ScheduleEntryResponse>();
            }
            weekDaysDictionary[entry.DayOfWeek.Value].Add(scheduleEntryResponse);
        }

        // Skapa en lista av ScheduleWeekDayResponse baserat på dictionaryn, sortera veckodagarna i rätt ordning.
        List<ScheduleWeekDayResponse>? scheduleWeekDays = weekDaysDictionary
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
}