using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class UpcomingEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures/upcoming", (FixtureData data, string? team) =>
        {
            var q = data.Matches.Where(m => !m.Played);

            if (team is not null)
                q = q.Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                              || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase));

            // Upcoming fixtures may have no date yet, so matchday is the only reliable order.
            return Results.Ok(q.OrderBy(m => m.Matchday).ToList());
        });
    }
}
