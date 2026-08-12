using System.Net;
using System.Net.Http.Json;
using FixturesApi.Data;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class TeamEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Team_ReturnsSummaryWithoutForm()
    {
        var response = await _client.GetAsync("/teams/Arsenal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.ReadAsAsync<TeamSummary>();
        Assert.Equal("Arsenal", summary.Team);
        Assert.Equal(summary.Played, summary.Won + summary.Drawn + summary.Lost);
        Assert.Equal(summary.Points, summary.Won * 3 + summary.Drawn);
        Assert.Equal(summary.GoalDifference, summary.GoalsFor - summary.GoalsAgainst);

        var body = await response.ReadJsonAsync();
        Assert.False(body.TryGetProperty("form", out _));
    }

    [Fact]
    public async Task Get_Team_WithForm_IncludesFormString()
    {
        var response = await _client.GetAsync("/teams/Arsenal?withForm=true");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Arsenal", body.GetProperty("team").GetString());

        var form = body.GetProperty("form").GetString();
        Assert.NotNull(form);
        Assert.All(form!.Split('-', StringSplitOptions.RemoveEmptyEntries),
            r => Assert.Contains(r, new[] { "W", "D", "L" }));
    }

    [Fact]
    public async Task Get_Team_WithFormFalse_MatchesPlainSummary()
    {
        var plain = await _client.GetFromJsonAsync<TeamSummary>("/teams/Liverpool", JsonHelpers.Options);
        var explicitlyOff = await _client.GetFromJsonAsync<TeamSummary>("/teams/Liverpool?withForm=false", JsonHelpers.Options);

        Assert.Equal(plain, explicitlyOff);
    }

    [Fact]
    public async Task Get_Team_IsCaseInsensitive()
    {
        var lower = await _client.GetFromJsonAsync<TeamSummary>("/teams/liverpool", JsonHelpers.Options);
        var proper = await _client.GetFromJsonAsync<TeamSummary>("/teams/Liverpool", JsonHelpers.Options);

        Assert.NotNull(lower);
        Assert.NotNull(proper);
        Assert.Equal(proper!.Played, lower!.Played);
        Assert.Equal(proper.Points, lower.Points);
    }

    [Fact]
    public async Task Get_Team_UnknownTeam_ReturnsZeroedSummary()
    {
        var summary = await _client.GetFromJsonAsync<TeamSummary>("/teams/Nowhere%20Rovers", JsonHelpers.Options);

        Assert.NotNull(summary);
        Assert.Equal("Nowhere Rovers", summary!.Team);
        Assert.Equal(0, summary.Played);
        Assert.Equal(0, summary.Points);
    }
}
