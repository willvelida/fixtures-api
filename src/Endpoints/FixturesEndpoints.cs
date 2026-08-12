using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class FixturesEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/fixtures", (FixtureData data, string? team, bool? played) =>
        {
            var q = data.Matches.AsEnumerable();
            if (team is not null)
                q = q.Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                              || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase));
            if (played is not null)
                q = q.Where(m => m.Played == played);
            return Results.Ok(q);
        });

        app.MapGet("/fixtures/export", (FixtureData data) =>
            Results.File(data.ToCsv(), "text/csv", "fixtures.csv"));

        app.MapGet("/fixtures/form", (FixtureData data) =>
        {
            const string team = "Arsenal";
            return Results.Ok(new { team, form = data.ComputeForm(team) });
        });
    }
}
