using System.Net;
using System.Net.Http.Json;
using FixturesApi.Data;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class UpcomingEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Upcoming_ReturnsOnlyUnplayedOrderedByMatchday()
    {
        var response = await _client.GetAsync("/fixtures/upcoming");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var matches = await response.ReadAsAsync<List<Match>>();
        Assert.NotEmpty(matches);
        Assert.All(matches, m => Assert.False(m.Played));
        Assert.Equal(matches.OrderBy(m => m.Matchday).Select(m => m.Matchday), matches.Select(m => m.Matchday));
    }

    [Fact]
    public async Task Get_Upcoming_FiltersByTeam()
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>(
            "/fixtures/upcoming?team=manchester%20city", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.NotEmpty(matches);
        Assert.All(matches, m =>
        {
            Assert.False(m.Played);
            Assert.True(m.Home.Equals("Manchester City", StringComparison.OrdinalIgnoreCase)
                     || m.Away.Equals("Manchester City", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task Get_Upcoming_UnknownTeam_ReturnsEmptyList()
    {
        var matches = await _client.GetFromJsonAsync<List<Match>>(
            "/fixtures/upcoming?team=Nowhere%20Rovers", JsonHelpers.Options);

        Assert.NotNull(matches);
        Assert.Empty(matches);
    }
}
