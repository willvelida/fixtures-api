using System.Net;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class DerbiesEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Derbies_ReturnsOnlyMatchesAgainstKnownRivals()
    {
        var response = await _client.GetAsync("/fixtures/derbies?team=Arsenal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Arsenal", body.GetProperty("team").GetString());

        var rivals = body.GetProperty("rivals").EnumerateArray().Select(r => r.GetString()!).ToList();
        Assert.NotEmpty(rivals);

        var matches = body.GetProperty("matches").EnumerateArray().ToList();
        Assert.Equal(matches.Count, body.GetProperty("count").GetInt32());
        Assert.NotEmpty(matches);

        foreach (var m in matches)
        {
            var home = m.GetProperty("home").GetString()!;
            var away = m.GetProperty("away").GetString()!;
            var opponent = home == "Arsenal" ? away : home;

            Assert.Contains("Arsenal", new[] { home, away });
            Assert.Contains(opponent, rivals);
        }
    }

    [Fact]
    public async Task Get_Derbies_MatchesAreOrderedByMatchday()
    {
        var body = await (await _client.GetAsync("/fixtures/derbies?team=Everton")).ReadJsonAsync();

        var matchdays = body.GetProperty("matches").EnumerateArray()
            .Select(m => m.GetProperty("matchday").GetInt32())
            .ToList();

        Assert.Equal(matchdays.OrderBy(d => d), matchdays);
    }

    [Fact]
    public async Task Get_Derbies_TeamLookupIsCaseInsensitive()
    {
        var response = await _client.GetAsync("/fixtures/derbies?team=liverpool");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Contains("Everton", body.GetProperty("rivals").EnumerateArray().Select(r => r.GetString()));
    }

    [Theory]
    [InlineData("/fixtures/derbies")]
    [InlineData("/fixtures/derbies?team=%20")]
    public async Task Get_Derbies_MissingTeam_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("The 'team' query parameter is required.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_Derbies_UnknownTeam_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/fixtures/derbies?team=Nowhere%20Rovers");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("No known derby rivals for 'Nowhere Rovers'.", body.GetProperty("error").GetString());
    }
}
