using System.Net;
using System.Net.Http.Json;
using FixturesApi.Data;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class TableEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Table_ReturnsStandingsOrderedByPosition()
    {
        var response = await _client.GetAsync("/table");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var standings = await response.ReadAsAsync<List<Standing>>();
        Assert.NotEmpty(standings);
        Assert.Equal(standings.OrderBy(s => s.Position).Select(s => s.Position), standings.Select(s => s.Position));
        Assert.Equal(1, standings[0].Position);
    }

    [Fact]
    public async Task Get_Table_StandingsAreInternallyConsistent()
    {
        var standings = await _client.GetFromJsonAsync<List<Standing>>("/table", JsonHelpers.Options);

        Assert.NotNull(standings);
        Assert.All(standings, s =>
        {
            Assert.Equal(s.Played, s.Won + s.Drawn + s.Lost);
            Assert.Equal(s.Points, s.Won * 3 + s.Drawn);
            Assert.Equal(s.Gd, s.Gf - s.Ga);
        });
    }

    [Fact]
    public async Task Get_TablePosition_ReturnsArsenalStanding()
    {
        var response = await _client.GetAsync("/table/position");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var standing = await response.ReadAsAsync<Standing>();
        Assert.Equal("Arsenal", standing.Team);
        Assert.True(standing.Position >= 1);
    }
}
