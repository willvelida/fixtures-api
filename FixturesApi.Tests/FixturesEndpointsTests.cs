using System.Net;
using System.Net.Http.Json;
using FixturesApi.Data;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class FixturesEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Fixtures_ReturnsAllMatches()
    {
        var response = await _client.GetAsync("/fixtures");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matches = await response.ReadAsAsync<List<Match>>();
        Assert.NotEmpty(matches);
        Assert.All(matches, m =>
        {
            Assert.False(string.IsNullOrWhiteSpace(m.Home));
            Assert.False(string.IsNullOrWhiteSpace(m.Away));
        });
    }

    [Fact]
    public async Task Get_Fixtures_FiltersByTeam_CaseInsensitively()
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>("/fixtures?team=arsenal", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        Assert.All(matches, m =>
            Assert.True(m.Home.Equals("Arsenal", StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals("Arsenal", StringComparison.OrdinalIgnoreCase)));
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Get_Fixtures_FiltersByPlayed(bool played)
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>(
            $"/fixtures?played={played.ToString().ToLowerInvariant()}", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        Assert.All(matches, m => Assert.Equal(played, m.Played));
    }

    [Fact]
    public async Task Get_Fixtures_CombinesTeamAndPlayedFilters()
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>(
            "/fixtures?team=Chelsea&played=true", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.All(matches, m =>
        {
            Assert.True(m.Played);
            Assert.True(m.Home.Equals("Chelsea", StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals("Chelsea", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task Get_Fixtures_UnknownTeam_ReturnsEmptyList()
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>(
            "/fixtures?team=Nowhere%20Rovers", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.Empty(matches);
    }

    [Fact]
    public async Task Get_FixturesExport_ReturnsCsvWithHeaderAndRowPerMatch()
    {
        var response = await _client.GetAsync("/fixtures/export");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/csv", response.Content.Headers.ContentType?.MediaType);

        var csv = await response.Content.ReadAsStringAsync();
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        Assert.Equal("season,matchday,date,home,away,homeScore,awayScore,played", lines[0].TrimEnd('\r'));

        var matches = await _client.GetFromJsonAsync<List<Match>>("/fixtures", JsonHelpers.Options);
        Assert.Equal(matches!.Count + 1, lines.Length);
    }

    [Fact]
    public async Task Get_FixturesForm_ReturnsArsenalFormString()
    {
        var response = await _client.GetAsync("/fixtures/form");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Arsenal", body.GetProperty("team").GetString());

        var form = body.GetProperty("form").GetString();
        Assert.NotNull(form);
        var results = form!.Split('-', StringSplitOptions.RemoveEmptyEntries);
        Assert.InRange(results.Length, 1, 5);
        Assert.All(results, r => Assert.Contains(r, new[] { "W", "D", "L" }));
    }
}
