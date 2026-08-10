using System.Globalization;
using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class CongestionEndpoints : IEndpointModule
{
    // A window is "congested" when it holds noticeably more matches than the team's own
    // average density over the same span, so the bar scales with how busy the team already is.
    const double CongestionFactor = 1.5;
    const int MinimumCongestedMatches = 2;
    const int MaxWindowDays = 365;

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/congestion", (FixtureData data, string? team, int? days) =>
        {
            if (string.IsNullOrWhiteSpace(team))
                return Results.BadRequest(new { error = "'team' is required." });

            if (days is null)
                return Results.BadRequest(new { error = "'days' is required." });

            if (days < 1 || days > MaxWindowDays)
                return Results.BadRequest(new { error = $"'days' must be between 1 and {MaxWindowDays}." });

            var window = days.Value;

            // Undated fixtures can't sit in a calendar window, so they're excluded outright.
            var dated = data.Matches
                .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                         || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
                .Select(m => (Match: m, Date: ParseDate(m.Date)))
                .Where(x => x.Date is not null)
                .Select(x => (x.Match, Date: x.Date!.Value))
                .OrderBy(x => x.Date)
                .ThenBy(x => x.Match.Matchday)
                .ToList();

            var expected = ExpectedPerWindow(dated.Select(x => x.Date).ToList(), window);
            var threshold = Math.Max(MinimumCongestedMatches, (int)Math.Ceiling(expected * CongestionFactor));

            var windows = new List<object>();
            var lastEndIndex = -1;

            for (var start = 0; start < dated.Count; start++)
            {
                var windowStart = dated[start].Date;
                var windowEnd = windowStart.AddDays(window - 1);

                var end = start;
                while (end + 1 < dated.Count && dated[end + 1].Date <= windowEnd)
                    end++;

                var count = end - start + 1;
                if (count < threshold) continue;

                // Later starts that stop at the same match only re-report a subset, so skip them.
                if (end == lastEndIndex) continue;
                lastEndIndex = end;

                windows.Add(new
                {
                    start = windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    end = windowEnd.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    firstMatchDate = windowStart.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    lastMatchDate = dated[end].Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    matchCount = count,
                    matches = dated.GetRange(start, count).Select(x => x.Match).ToList()
                });
            }

            return Results.Ok(new
            {
                team,
                days = window,
                threshold,
                expectedPerWindow = Math.Round(expected, 2),
                datedMatches = dated.Count,
                count = windows.Count,
                windows
            });
        });
    }

    // Average matches the team plays in any `days` stretch across its dated season span.
    static double ExpectedPerWindow(IReadOnlyList<DateOnly> dates, int days)
    {
        if (dates.Count == 0) return 0;

        var span = dates[^1].DayNumber - dates[0].DayNumber + 1;
        return dates.Count * (double)days / span;
    }

    static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;
}
