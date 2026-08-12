using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class DerbiesEndpoints : IEndpointModule
{
    // Rivalries live here rather than in the seed data, so the lookup stays local to this endpoint.
    static readonly IReadOnlyDictionary<string, string[]> Rivals =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arsenal"] = ["Tottenham Hotspur", "Chelsea", "West Ham United"],
            ["Aston Villa"] = ["Wolverhampton Wanderers", "Coventry City"],
            ["Brentford"] = ["Fulham", "Chelsea"],
            ["Brighton & Hove Albion"] = ["Crystal Palace"],
            ["Chelsea"] = ["Tottenham Hotspur", "Arsenal", "Fulham", "West Ham United", "Brentford"],
            ["Coventry City"] = ["Aston Villa", "Wolverhampton Wanderers"],
            ["Crystal Palace"] = ["Brighton & Hove Albion"],
            ["Everton"] = ["Liverpool"],
            ["Fulham"] = ["Chelsea", "Brentford"],
            ["Hull City"] = ["Leeds United"],
            ["Leeds United"] = ["Manchester United", "Hull City"],
            ["Liverpool"] = ["Everton", "Manchester United"],
            ["Manchester City"] = ["Manchester United"],
            ["Manchester United"] = ["Manchester City", "Liverpool", "Leeds United"],
            ["Newcastle United"] = ["Sunderland"],
            ["Sunderland"] = ["Newcastle United"],
            ["Tottenham Hotspur"] = ["Arsenal", "Chelsea", "West Ham United"],
            ["West Ham United"] = ["Tottenham Hotspur", "Chelsea", "Arsenal"],
            ["Wolverhampton Wanderers"] = ["Aston Villa", "Coventry City"]
        };

    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/derbies", (FixtureData data, string? team) =>
        {
            if (string.IsNullOrWhiteSpace(team))
                return Results.BadRequest(new { error = "The 'team' query parameter is required." });

            if (!Rivals.TryGetValue(team, out var rivals))
                return Results.NotFound(new { error = $"No known derby rivals for '{team}'." });

            var matches = data.Matches
                .Where(m => IsDerby(m, team, rivals))
                .OrderBy(m => m.Matchday)
                .ToList();

            return Results.Ok(new { team, rivals, count = matches.Count, matches });
        });
    }

    static bool IsDerby(Match m, string team, string[] rivals)
    {
        if (m.Home.Equals(team, StringComparison.OrdinalIgnoreCase))
            return rivals.Contains(m.Away, StringComparer.OrdinalIgnoreCase);

        if (m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
            return rivals.Contains(m.Home, StringComparer.OrdinalIgnoreCase);

        return false;
    }
}
