using FixturesApi.Data;

namespace FixturesApi.Endpoints;

public class TeamEndpoints : IEndpointModule
{
    public void MapEndpoints(IEndpointRouteBuilder app)
    {
        app.MapGet("/teams/{name}", (FixtureData data, string name, bool? withForm) =>
        {
            var summary = data.Summarize(name);
            if (withForm != true) return Results.Ok(summary);

            return Results.Ok(new
            {
                summary.Team,
                summary.Played,
                summary.Won,
                summary.Drawn,
                summary.Lost,
                summary.GoalsFor,
                summary.GoalsAgainst,
                summary.GoalDifference,
                summary.Points,
                Form = data.ComputeForm(name)
            });
        });
    }
}
