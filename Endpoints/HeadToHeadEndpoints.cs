using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class HeadToHeadEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/h2h", (FixtureData data, string? teamA, string? teamB) =>
        {
            if (string.IsNullOrWhiteSpace(teamA) || string.IsNullOrWhiteSpace(teamB))
                return Results.BadRequest(new { error = "Both 'teamA' and 'teamB' are required." });

            var matches = data.Matches
                .Where(m => IsPairing(m, teamA, teamB))
                .ToList();

            return Results.Ok(new { teamA, teamB, count = matches.Count, matches });
        });
    }

    static bool IsPairing(Match m, string teamA, string teamB) =>
        (m.Home.Equals(teamA, StringComparison.OrdinalIgnoreCase)
         && m.Away.Equals(teamB, StringComparison.OrdinalIgnoreCase))
        || (m.Home.Equals(teamB, StringComparison.OrdinalIgnoreCase)
            && m.Away.Equals(teamA, StringComparison.OrdinalIgnoreCase));
}
