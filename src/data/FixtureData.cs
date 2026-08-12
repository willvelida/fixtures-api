using System.Globalization;
using System.Text;
using System.Text.Json;

namespace FixturesApi.Data;

// Loads the seed once and owns every read/derivation over it, so endpoints stay thin.
public class FixtureData
{
    public IReadOnlyList<Match> Matches { get; }
    public IReadOnlyList<Standing> Standings { get; }

    public FixtureData(IHostEnvironment env)
    {
        var seedPath = Path.Combine(env.ContentRootPath, "seed", "fixtures.json");
        var seed = JsonSerializer.Deserialize<SeedData>(
            File.ReadAllText(seedPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        Matches = seed.Matches;
        Standings = seed.Standings;
    }

    public Standing? Position(string team) =>
        Standings.FirstOrDefault(s => s.Team.Equals(team, StringComparison.OrdinalIgnoreCase));

    public string ComputeForm(string team)
    {
        var recent = Matches
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

    public TeamSummary Summarize(string name)
    {
        var played = Matches
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

    public byte[] ToCsv()
    {
        var csv = new StringBuilder();
        csv.AppendLine("season,matchday,date,home,away,homeScore,awayScore,played");
        foreach (var m in Matches)
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
        return Encoding.UTF8.GetBytes(csv.ToString());
    }

    static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
