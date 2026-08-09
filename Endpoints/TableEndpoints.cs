using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class TableEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/table/position", (FixtureData data) =>
        {
            var arsenal = data.Position("Arsenal");
            return arsenal is null ? Results.NotFound() : Results.Ok(arsenal);
        });
    }
}
