using System.Globalization;
using System.Text;
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

app.MapGet("/fixtures/export", () =>
{
    var csv = new StringBuilder();
    csv.AppendLine("season,matchday,date,home,away,homeScore,awayScore,played");
    foreach (var m in data.Matches)
    {
        csv.AppendLine(string.Join(',',
            Escape(m.Season),
            m.Matchday.ToString(CultureInfo.InvariantCulture),
            Escape(m.Date),
            Escape(m.Home),
            Escape(m.Away),
            m.HomeScore?.ToString(CultureInfo.InvariantCulture) ?? "",
            m.AwayScore?.ToString(CultureInfo.InvariantCulture) ?? "",
            m.Played ? "true" : "false"));
    }

    return Results.File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "fixtures.csv");
});

app.MapGet("/table/position", () =>
{
    var arsenal = data.Standings
        .FirstOrDefault(s => s.Team.Equals("Arsenal", StringComparison.OrdinalIgnoreCase));
    return arsenal is null ? Results.NotFound() : Results.Ok(arsenal);
});

string ComputeForm(string team)
{
    var recent = data.Matches
        .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
        .Where(m => m.Home.Equals(team, StringComparison.OrdinalIgnoreCase)
                 || m.Away.Equals(team, StringComparison.OrdinalIgnoreCase))
        .OrderByDescending(m => DateOnly.Parse(m.Date))
        .ThenByDescending(m => m.Matchday)
        .Take(5)
        .Reverse()
        .ToList();

    return string.Join("-", recent.Select(m =>
    {
        var isHome = m.Home.Equals(team, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;
        return scored > conceded ? "W" : scored == conceded ? "D" : "L";
    }));
}

app.MapGet("/fixtures/form", () =>
{
    const string team = "Arsenal";
    return Results.Ok(new { team, form = ComputeForm(team) });
});

TeamSummary SummarizeTeam(string name)
{
    var played = data.Matches
        .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
        .Where(m => m.Home.Equals(name, StringComparison.OrdinalIgnoreCase)
                 || m.Away.Equals(name, StringComparison.OrdinalIgnoreCase))
        .ToList();

    int won = 0, drawn = 0, lost = 0, goalsFor = 0, goalsAgainst = 0;

    foreach (var m in played)
    {
        var isHome = m.Home.Equals(name, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;

        goalsFor += scored;
        goalsAgainst += conceded;

        if (scored > conceded) won++;
        else if (scored == conceded) drawn++;
        else lost++;
    }

    return new TeamSummary(name, played.Count, won, drawn, lost, goalsFor, goalsAgainst,
                           goalsFor - goalsAgainst, won * 3 + drawn);
}

app.MapGet("/teams/{name}", (string name, bool? withForm) =>
{
    var summary = SummarizeTeam(name);
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
        Form = ComputeForm(name)
    });
});

TeamSummary SummarizeTeam(string name)
{
    var played = data.Matches
        .Where(m => m.Played && m.HomeScore is not null && m.AwayScore is not null)
        .Where(m => m.Home.Equals(name, StringComparison.OrdinalIgnoreCase)
                 || m.Away.Equals(name, StringComparison.OrdinalIgnoreCase))
        .ToList();

    int won = 0, drawn = 0, lost = 0, goalsFor = 0, goalsAgainst = 0;

    foreach (var m in played)
    {
        var isHome = m.Home.Equals(name, StringComparison.OrdinalIgnoreCase);
        var scored = isHome ? m.HomeScore!.Value : m.AwayScore!.Value;
        var conceded = isHome ? m.AwayScore!.Value : m.HomeScore!.Value;

        goalsFor += scored;
        goalsAgainst += conceded;

        if (scored > conceded) won++;
        else if (scored == conceded) drawn++;
        else lost++;
    }

    return new TeamSummary(name, played.Count, won, drawn, lost, goalsFor, goalsAgainst,
                           goalsFor - goalsAgainst, won * 3 + drawn);
}

app.Run();

static string Escape(string? value)
{
    if (string.IsNullOrEmpty(value)) return "";
    if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        return $"\"{value.Replace("\"", "\"\"")}\"";
    return value;
}

record Match(string Season, int Matchday, string Date, string Home, string Away,
             int? HomeScore, int? AwayScore, bool Played);
record Standing(int Position, string Team, int Played, int Won, int Drawn, int Lost,
                int Gf, int Ga, int Gd, int Points);
record SeedData(List<Match> Matches, List<Standing> Standings);
record TeamSummary(string Team, int Played, int Won, int Drawn, int Lost,
                   int GoalsFor, int GoalsAgainst, int GoalDifference, int Points);