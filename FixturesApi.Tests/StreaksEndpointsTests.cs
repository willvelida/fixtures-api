using System.Net;
using System.Net.Http.Json;
using FixturesApi.Endpoints;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class StreaksEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Streaks_ReturnsStreaksOrderedByLengthDescending()
    {
        var response = await _client.GetAsync("/table/streaks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var streaks = await response.ReadAsAsync<List<TeamStreak>>();
        Assert.NotEmpty(streaks);
        Assert.Equal(streaks.OrderByDescending(s => s.Length).Select(s => s.Length), streaks.Select(s => s.Length));
    }

    [Fact]
    public async Task Get_Streaks_HasValidTypesAndPositiveLengths()
    {
        var streaks = await _client.GetFromJsonAsync<List<TeamStreak>>("/table/streaks", JsonHelpers.Options);

        Assert.NotNull(streaks);
        Assert.All(streaks, s =>
        {
            Assert.Contains(s.Type, new[] { "win", "draw", "loss" });
            Assert.True(s.Length >= 1);
            Assert.False(string.IsNullOrWhiteSpace(s.Team));
        });
    }

    [Fact]
    public async Task Get_Streaks_ReturnsOneEntryPerTeamInTheTable()
    {
        var streaks = await _client.GetFromJsonAsync<List<TeamStreak>>("/table/streaks", JsonHelpers.Options);

        Assert.NotNull(streaks);
        var teams = streaks!.Select(s => s.Team).ToList();
        Assert.Equal(teams.Count, teams.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
