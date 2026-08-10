using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class DerbiesEndpoints : IEndpointModule
{
    // Kept local to this module so the rivalry lookup travels with the endpoint.
    static readonly IReadOnlyDictionary<string, string[]> Rivals =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arsenal"] = ["Tottenham Hotspur", "Chelsea", "West Ham United"],
            ["Tottenham Hotspur"] = ["Arsenal", "Chelsea", "West Ham United"],
            ["Chelsea"] = ["Arsenal", "Tottenham Hotspur", "Fulham", "West Ham United"],
            ["West Ham United"] = ["Tottenham Hotspur", "Arsenal", "Chelsea"],
            ["Fulham"] = ["Chelsea", "Brentford"],
            ["Brentford"] = ["Fulham", "Chelsea"],
            ["Crystal Palace"] = ["Brighton & Hove Albion"],
            ["Brighton & Hove Albion"] = ["Crystal Palace"],
            ["Manchester United"] = ["Manchester City", "Liverpool", "Leeds United"],
            ["Manchester City"] = ["Manchester United"],
            ["Liverpool"] = ["Everton", "Manchester United"],
            ["Everton"] = ["Liverpool"],
            ["Newcastle United"] = ["Sunderland"],
            ["Sunderland"] = ["Newcastle United"],
            ["Leeds United"] = ["Manchester United", "Hull City"],
            ["Hull City"] = ["Leeds United"],
            ["Aston Villa"] = ["Wolverhampton Wanderers", "Coventry City"],
            ["Wolverhampton Wanderers"] = ["Aston Villa"],
            ["Coventry City"] = ["Aston Villa"],
        };

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/derbies", (FixtureData data, string? team) =>
        {
            if (string.IsNullOrWhiteSpace(team))
                return Results.BadRequest(new { error = "'team' is required." });

            var rivals = Rivals.TryGetValue(team.Trim(), out var known) ? known : [];

            var matches = data.Matches
                .Where(m => IsDerby(m, team.Trim(), rivals))
                .OrderBy(m => m.Matchday)
                .ToList();

            return Results.Ok(new { team = team.Trim(), rivals, count = matches.Count, matches });
        });
    }

    static bool IsDerby(Match m, string team, string[] rivals)
    {
        if (rivals.Length == 0) return false;

        if (m.Home.Equals(team, StringComparison.OrdinalIgnoreCase))
            return rivals.Contains(m.Away, StringComparer.OrdinalIgnoreCase);

        if (m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
            return rivals.Contains(m.Home, StringComparer.OrdinalIgnoreCase);

        return false;
    }
}
