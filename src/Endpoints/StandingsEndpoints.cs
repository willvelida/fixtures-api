using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class StandingsEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/table", (FixtureData data) =>
            Results.Ok(data.Standings.OrderBy(s => s.Position)));
    }
}
