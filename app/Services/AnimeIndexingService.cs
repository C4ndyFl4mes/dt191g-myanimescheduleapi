using App.Data;
using App.DTOs;
using App.Enums;
using App.Models;
using Microsoft.EntityFrameworkCore;
using NodaTime;

namespace App.Services;

public class AnimeIndexingService : IHostedService, IDisposable
{
    private Timer? _timer;
    private HttpClient _http;
    private readonly IServiceProvider _serviceProvider;

    public AnimeIndexingService(IServiceProvider serviceProvider, HttpClient http)
    {
        _serviceProvider = serviceProvider;
        _http = http;
    }

    // Startar en timer som kör IndexAnimes-metoden var 24:e timme.
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new Timer(IndexAnimes, null, TimeSpan.Zero, TimeSpan.FromDays(1));
    }

    /** *
     * Hämtar data från Jikan API:et och indexerar animes som är airing eller inte har finished airing status.
     * För varje anime beräknas en automatic removal date baserat på aired.from + (episodes * 7 dagar).
     * Endast unika animes läggs till i databasen baserat på mal_id.
     * Animes som redan finns i databasen kommer inte att läggas till igen.
     */
    private async void IndexAnimes(object? state)
    {
        using var scope = _serviceProvider.CreateScope();

        ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); // Skapar en scope för att få en instans av ApplicationDbContext.
        int current_page = 1;
        Result? result = null;
        List<IndexableAnime> allAnimesToBeAdded = new List<IndexableAnime>();
        int gatheredAnimes = 0;

        do
        {
            result = await _http.GetFromJsonAsync<Result>($"https://api.jikan.moe/v4/seasons/now?page={current_page}") ?? throw new Exception("Error fetching result.");

            allAnimesToBeAdded.AddRange(result.data);
            gatheredAnimes += result.data.Count;
            Console.WriteLine($"{gatheredAnimes} animes fetched. Now on page {result.pagination.current_page}.");
            current_page++;
            await Task.Delay(3000); // Delay för att undvika att överbelasta API:et;

        } while (result!.pagination.has_next_page);

        // Ser till att endast unika animes läggs till i listan baserat på mal_id.
        allAnimesToBeAdded = allAnimesToBeAdded
            .GroupBy(anime => anime.mal_id)
            .Select(group => group.First())
            .ToList();

        Console.WriteLine($"Total unique animes to be added: {allAnimesToBeAdded.Count}.");

        if (allAnimesToBeAdded.Count != 0)
        {
            DateTime currentTime = DateTime.UtcNow;

            List<IndexedAnimeModel> animeList = [.. allAnimesToBeAdded
                .Select(anime => new IndexedAnimeModel
                {
                    Mal_ID = anime.mal_id,
                    Title = anime.titles.FirstOrDefault(t => t.type == "English")?.title ?? anime.titles.FirstOrDefault(t => t.type == "Default")?.title ?? "Unknown Title",
                    ImageURL = anime.images.webp.image_url,
                    Status = anime.status,
                    TotalEpisodes = anime.episodes ?? null,
                    ReleaseInstant = GetBroadcastInstant(anime.aired.from, anime.broadcast?.time, anime.broadcast?.timezone)
                })];

            List<IndexedAnimeModel> existingAnimes = await context.IndexedAnimes.ToListAsync();

            // Lägger endast till nya animes som inte redan finns i databasen.
            List<IndexedAnimeModel>? newAnimes = animeList.Where(anime => !existingAnimes.Any(existing => existing.Mal_ID == anime.Mal_ID)).ToList();

            if (newAnimes == null || newAnimes.Count == 0)
            {
                Console.WriteLine("No new animes to add to the database.");
                return;
            }

            if (newAnimes.Count > 0)
            {
                await context.IndexedAnimes.AddRangeAsync(newAnimes);
                Console.WriteLine($"Added {newAnimes.Count} new animes to the database.");
            }
            else
            {
                Console.WriteLine("No new animes to add to the database.");
            }

            try
            {
                if (context.ChangeTracker.HasChanges())
                {
                    Console.WriteLine("Saving changes to the database...");
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving changes to the database: {ex.Message}");
            }

        }
    }

    // Stoppar timern när tjänsten stoppas.
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    // Disponerar timern när tjänsten förstörs.
    public void Dispose()
    {
        _timer?.Dispose();
    }

    // Hjälpmetod för att beräkna release instant baserat på aired.from, broadcast time och timezone.
    private Instant GetBroadcastInstant(string airedFromDate, string? broadcastTime, string? broadcastTimezone)
    {
        // Parsar aired.from datumet, om det inte går så används nuvarande UTC tid.
        if (!DateTime.TryParse(airedFromDate, out var releaseDate))
        {
            releaseDate = DateTime.UtcNow;
        }

        // Parsar broadcast time (standardvärde 00:00 om det inte finns)
        var time = "00:00";
        if (!string.IsNullOrEmpty(broadcastTime))
        {
            time = broadcastTime;
        }

        // Kombinerar datum och tid
        var dateTimeString = $"{releaseDate:yyyy-MM-dd} {time}";
        if (!DateTime.TryParse(dateTimeString, out var localDateTime))
        {
            localDateTime = releaseDate;
        }

        // Behandlar den parsade datetime som den angivna tidszonen (standardvärde JST, Asia/Tokyo) och konverterar till Instant
        var jstZone = DateTimeZoneProviders.Tzdb[broadcastTimezone ?? "Asia/Tokyo"];
        var zonedDateTime = jstZone.AtStrictly(LocalDateTime.FromDateTime(localDateTime));
        return zonedDateTime.ToInstant();
    }

}