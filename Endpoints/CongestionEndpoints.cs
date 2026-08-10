using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class CongestionEndpoints : IEndpointModule
{
    const int DefaultDays = 14;
    const int PileUpThreshold = 3;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/congestion", (FixtureData data, string? team, int? days) =>
        {
            if (string.IsNullOrWhiteSpace(team))
                return Results.BadRequest(new { error = "'team' is required." });

            var windowDays = days ?? DefaultDays;
            if (windowDays < 1)
                return Results.BadRequest(new { error = "'days' must be at least 1." });

            var fixtures = data.Matches
                .Where(m => !m.Played)
                .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                         || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
                // Unplayed fixtures may not be scheduled yet, so they carry no parsable date.
                .Where(m => !string.IsNullOrWhiteSpace(m.Date))
                .Select(m => (Match: m, Date: DateOnly.Parse(m.Date)))
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Match.Matchday)
                .ToList();

            var windows = FindPileUps(fixtures, windowDays);

            return Results.Ok(new
            {
                team,
                days = windowDays,
                scheduledFixtures = fixtures.Count,
                undatedFixtures = data.Matches.Count(m => !m.Played
                    && (m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
                    && string.IsNullOrWhiteSpace(m.Date)),
                count = windows.Count,
                windows
            });
        });
    }

    static List<object> FindPileUps(List<(Match Match, DateOnly Date)> fixtures, int windowDays)
    {
        var results = new List<object>();
        var coveredThrough = -1;

        for (var start = 0; start < fixtures.Count; start++)
        {
            var end = start;
            while (end + 1 < fixtures.Count
                   && fixtures[end + 1].Date.DayNumber - fixtures[start].Date.DayNumber < windowDays)
                end++;

            var size = end - start + 1;
            if (size < PileUpThreshold) continue;

            // A window already reported covers these fixtures, so it would only repeat it.
            if (end <= coveredThrough) continue;
            coveredThrough = end;

            var slice = fixtures.GetRange(start, size);
            results.Add(new
            {
                windowStart = fixtures[start].Date.ToString("yyyy-MM-dd"),
                windowEnd = fixtures[end].Date.ToString("yyyy-MM-dd"),
                spanDays = fixtures[end].Date.DayNumber - fixtures[start].Date.DayNumber + 1,
                matchCount = size,
                matches = slice.Select(x => x.Match).ToList()
            });
        }

        return results;
    }
}
