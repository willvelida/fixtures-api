using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public record TeamStreak(string Team, string Type, int Length);

public class StreaksEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/table/streaks", (FixtureData data) =>
        {
            var streaks = TeamNames(data)
                .Select(team => BuildStreak(data, team))
                .Where(s => s is not null)
                .Select(s => s!)
                .OrderByDescending(s => s.Length)
                .ThenBy(s => s.Team, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return Results.Ok(streaks);
        });
    }

    static IEnumerable<string> TeamNames(FixtureData data)
    {
        var fromStandings = data.Standings.OrderBy(s => s.Position).Select(s => s.Team);
        var fromMatches = data.Matches.SelectMany(m => new[] { m.Home, m.Away });

        return fromStandings.Concat(fromMatches).Distinct(StringComparer.OrdinalIgnoreCase);
    }

    static TeamStreak? BuildStreak(FixtureData data, string team)
    {
        var results = data.Matches
            .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
            .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(m => DateOnly.Parse(m.Date))
            .ThenByDescending(m => m.Matchday)
            .Select(m => ResultFor(m, team))
            .ToList();

        if (results.Count == 0) return null;

        var latest = results[0];
        var length = results.TakeWhile(r => r == latest).Count();

        return new TeamStreak(team, latest, length);
    }

    static string ResultFor(Match m, string team)
    {
        var isHome = m.Home.Equals(team, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;

        return scored > conceded ? "win" : scored == conceded ? "draw" : "loss";
    }
}
