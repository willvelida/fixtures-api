using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class StreaksEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/table/streaks", (FixtureData data) =>
        {
            var teams = data.Matches
                .SelectMany(m => new[] { m.Home, m.Away })
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

            var streaks = teams.Select(team => Streak(data, team)).ToList();

            return Results.Ok(streaks);
        });
    }

    static object Streak(FixtureData data, string team)
    {
        var recent = data.Matches
            .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
            .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => DateOnly.Parse(m.Date))
            .ThenByDescending(m => m.Matchday)
            .ToList();

        string? current = null;
        var length = 0;

        foreach (var m in recent)
        {
            var outcome = Outcome(m, team);
            if (current is null) current = outcome;
            else if (current != outcome) break;
            length++;
        }

        return new
        {
            team,
            result = current,
            length,
            streak = current is null ? "" : $"{length}{current}"
        };
    }

    static string Outcome(Match m, string team)
    {
        var isHome = m.Home.Equals(team, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;
        return scored > conceded ? "W" : scored == conceded ? "D" : "L";
    }
}
