using System.Diagnostics;
using App.Data;
using App.DTOs;
using App.Enums;
using App.Models;
using App.Records;
using Microsoft.EntityFrameworkCore;
using NodaTime;
using NodaTime.Text;

namespace App.Services;

// Hanterar periodisk indexering av anime från Jikan API.
public class AnimeIndexingBGService(ILogger<AnimeIndexingBGService> _logger, IServiceScopeFactory _scopeFactory, HttpClient _http) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(TimeSpan.FromDays(1));
        do
        {
            try
            {
                await StartIndexingAnimes(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while executing anime indexing background service.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested);
    }

    // Indexerar alla animes, samt uppdaterar och raderar om det behövs.
    private async Task StartIndexingAnimes(CancellationToken token)
    {
        long startTime = Stopwatch.GetTimestamp();
        using IServiceScope scope = _scopeFactory.CreateScope();
        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // Hämtar animes från Jikan API.
        int currentPage = 1;
        Result? result;
        Dictionary<int, PendingAnime> pendingAnimes = [];

        do
        {
            result = await _http.GetFromJsonAsync<Result>($"https://api.jikan.moe/v4/seasons/now?continuing&page={currentPage}");
            if (result == null)
            {
                break; // Om det blir null hoppar vi ur do-while-loopen för att sedan hamna i if-satsen under loopen.
            }

            for (int i = 0; i < result.data.Count; i++)
            {
                IndexableAnime anime = result.data[i];

                Instant? releaseInstant = GetBroadcastInstant(anime.aired.from, anime.broadcast?.time, anime.broadcast?.timezone);

                if (releaseInstant == null)
                {
                    continue; // Skippar denna anime då det antagligen blev något fel i GetBroadcastInstant exempelvis att anime.aired.from är ogiltig.
                }

                // Extraherar broadcast weekday från broadcast data.
                EWeekday? broadcastWeekday = null;
                if (anime.broadcast?.day != null)
                {
                    broadcastWeekday = anime.broadcast.day.ToLower() switch
                    {
                        "monday" or "mondays" => EWeekday.Monday,
                        "tuesday" or "tuesdays" => EWeekday.Tuesday,
                        "wednesday" or "wednesdays" => EWeekday.Wednesday,
                        "thursday" or "thursdays" => EWeekday.Thursday,
                        "friday" or "fridays" => EWeekday.Friday,
                        "saturday" or "saturdays" => EWeekday.Saturday,
                        "sunday" or "sundays" => EWeekday.Sunday,
                        _ => null
                    };
                }

                try
                {
                    // Lägger till en anime i pending dictionary.
                    pendingAnimes.Add(anime.mal_id, new PendingAnime
                    {
                        Mal_ID = anime.mal_id,
                        Title = anime.titles.FirstOrDefault(t => t.type == "English")?.title ?? anime.titles.FirstOrDefault(t => t.type == "Default")?.title ?? "Unknown Title",
                        ImageURL = anime.images.webp.image_url,
                        Status = anime.status,
                        TotalEpisodes = anime.episodes,
                        ReleaseInstant = (Instant)releaseInstant,
                        BroadcastWeekday = broadcastWeekday
                    });
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Tried to add an anime with the same Mal_ID in the current indexing schedule. Action taken: skip the current iteration.");
                    continue;
                }
            }

            Console.WriteLine($"{pendingAnimes.Count} animes is now pending. Progress: {result.pagination.current_page}/{result.pagination.last_visible_page}. Time Elapsed: {Stopwatch.GetElapsedTime(startTime)}");
            currentPage++;

            await Task.Delay(1000); // API:ets rate limit är 3 req/s och 60 req/min. 
        }
        while (result!.pagination.has_next_page);

        // Avbryter indexeringen.
        if (result == null)
        {
            throw new Exception("Unable to fetch animes from Jikan API.");
        }

        List<IndexedAnimeModel> existingAnimes = await context.IndexedAnimes.AsNoTracking().ToListAsync(token);
        List<IndexedAnimeModel> pendingUpdates = [];
        List<IndexedAnimeModel> pendingInsertions = [];
        int unchangedRows = 0;
        int animesFlagedForFinishedAiring = 0;

        foreach (IndexedAnimeModel existing in existingAnimes)
        {
            if (!pendingAnimes.TryGetValue(existing.Mal_ID, out PendingAnime? pending))
            {
                continue; // Om anime inte har lagts till i databasen ännu skippas denna iteration.
            }

            pendingAnimes.Remove(pending.Mal_ID); // Raderar alla animes from pendingAnimes som redan finns i databasen.

            // Kollar om någonting har ändrats.
            if (
                existing.Title != pending.Title ||
                existing.ImageURL != pending.ImageURL ||
                existing.Status != pending.Status ||
                existing.TotalEpisodes != pending.TotalEpisodes ||
                existing.ReleaseInstant != pending.ReleaseInstant ||
                existing.BroadcastWeekday != pending.BroadcastWeekday
            )
            {
                pendingUpdates.Add(new()
                {
                    Id = existing.Id,
                    Mal_ID = pending.Mal_ID,
                    Title = pending.Title,
                    ImageURL = pending.ImageURL,
                    Status = pending.Status,
                    TotalEpisodes = pending.TotalEpisodes,
                    ReleaseInstant = pending.ReleaseInstant,
                    BroadcastWeekday = pending.BroadcastWeekday
                });
                if (pending.Status == EStatus.FinishedAiring)
                {
                    animesFlagedForFinishedAiring++;
                }
            }
            else
            {
                unchangedRows++;
                continue; // Om en anime inte har något att uppdatera skippas denna iteration.
            }
        }

        // Lägger till alla animes som inte är FinishedAiring.
        pendingInsertions.AddRange(pendingAnimes.Values.Where(pI => pI.Status != EStatus.FinishedAiring).Select(p => new IndexedAnimeModel
        {
            Mal_ID = p.Mal_ID,
            Title = p.Title,
            ImageURL = p.ImageURL,
            Status = p.Status,
            TotalEpisodes = p.TotalEpisodes,
            ReleaseInstant = p.ReleaseInstant,
            BroadcastWeekday = p.BroadcastWeekday
        }));

        // Förbereder alla databasoperationer.
        if (pendingInsertions.Count > 0)
        {
            context.IndexedAnimes.AddRange(pendingInsertions);
        }

        if (pendingUpdates.Count > 0)
        {
            context.UpdateRange(pendingUpdates);
        }

        if (animesFlagedForFinishedAiring > 0)
        {
            await context.IndexedAnimes.Where(ia => ia.Status == EStatus.FinishedAiring).ExecuteDeleteAsync(token);
        }

        try
        {
            int totalChanges = await context.SaveChangesAsync(token);
            Console.WriteLine($"Total changes: {totalChanges} | New entries: {pendingInsertions.Count} | Updated entries: {pendingUpdates.Count} | Deleted entries: {animesFlagedForFinishedAiring} | Unchanged entries: {unchangedRows}. Total Elapsed time: {Stopwatch.GetElapsedTime(startTime)}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tried saving to database, but something went wrong.");
            return;
        }
    }

    // Hämtar en Instant för release. Konverterar airedFromDate till Asia/Tokyo (eller om annan broadcastTimezone är given) med eventuell broadcasting tid.
    private Instant? GetBroadcastInstant(string airedFromDate, string? broadcastTime, string? broadcastTimezone)
    {
        try
        {
            // Default värden.
            broadcastTime ??= "00:00";
            broadcastTimezone ??= "Asia/Tokyo";

            // Tidszon Asia/Tokyo
            DateTimeZone zone = DateTimeZoneProviders.Tzdb[broadcastTimezone];

            // Parsar airedFromDate till ett DateTime objekt.
            if (!DateTime.TryParse(airedFromDate, out var releaseDate))
            {
                _logger.LogWarning("Could not parse aired date: {AiredFromDate}.", airedFromDate);
                return null;
            }

            // Parsar tiden till korrekt sändningstid.
            LocalTime time = LocalTimePattern.CreateWithInvariantCulture("HH:mm").Parse(broadcastTime).Value;

            // Lokal datum och tid för sändarens tidszon.
            LocalDateTime localDateTime = LocalDate.FromDateTime(releaseDate) + time;
            ZonedDateTime zoned = localDateTime.InZoneLeniently(zone);

            return zoned.ToInstant();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse anime broadcast time: {AiredFromDate}.", airedFromDate);
            return null;
        }
    }
}