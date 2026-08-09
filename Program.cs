using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var seedPath = Path.Combine(builder.Environment.ContentRootPath, "seed", "fixtures.json");
var data = JsonSerializer.Deserialize<SeedData>(
    File.ReadAllText(seedPath),
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

app.MapGet("/fixtures", (string? team, bool? played) =>
{
    var q = data.Matches.AsEnumerable();
    if (team is not null)
        q = q.Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                      || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase));
    if (played is not null)
        q = q.Where(m => m.Played == played);
    return Results.Ok(q);
});

app.MapGet("/fixtures/form", () =>
{
    const string team = "Arsenal";

    var recent = data.Matches
        .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
        .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                 || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(m => DateOnly.Parse(m.Date))
        .ThenByDescending(m => m.Matchday)
        .Take(5)
        .Reverse()
        .ToList();

    var form = string.Join("-", recent.Select(m =>
    {
        var isHome = m.Home.Equals(team, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;
        return scored > conceded ? "W" : scored == conceded ? "D" : "L";
    }));

    return Results.Ok(new { team, form });
});

app.Run();

record Match(string Season, int Matchday, string Date, string Home, string Away,
             int? HomeScore, int? AwayScore, bool Played);
record Standing(int Position, string Team, int Played, int Won, int Drawn, int Lost,
                int Gf, int Ga, int Gd, int Points);
record SeedData(List<Match> Matches, List<Standing> Standings);