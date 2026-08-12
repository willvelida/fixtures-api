using System.Net;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class HeadToHeadEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_H2H_ReturnsOnlyMatchesBetweenTheTwoTeams()
    {
        var response = await _client.GetAsync("/fixtures/h2h?teamA=Arsenal&teamB=Chelsea");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Arsenal", body.GetProperty("teamA").GetString());
        Assert.Equal("Chelsea", body.GetProperty("teamB").GetString());

        var matches = body.GetProperty("matches").EnumerateArray().ToList();
        Assert.Equal(matches.Count, body.GetProperty("count").GetInt32());
        Assert.NotEmpty(matches);

        foreach (var m in matches)
        {
            var pair = new[] { m.GetProperty("home").GetString(), m.GetProperty("away").GetString() };
            Assert.Contains("Arsenal", pair);
            Assert.Contains("Chelsea", pair);
        }
    }

    [Fact]
    public async Task Get_H2H_IsOrderIndependent()
    {
        var forward = await _client.GetAsync("/fixtures/h2h?teamA=Arsenal&teamB=Chelsea");
        var reverse = await _client.GetAsync("/fixtures/h2h?teamA=Chelsea&teamB=Arsenal");

        var forwardCount = (await forward.ReadJsonAsync()).GetProperty("count").GetInt32();
        var reverseCount = (await reverse.ReadJsonAsync()).GetProperty("count").GetInt32();

        Assert.Equal(forwardCount, reverseCount);
    }

    [Theory]
    [InlineData("/fixtures/h2h")]
    [InlineData("/fixtures/h2h?teamA=Arsenal")]
    [InlineData("/fixtures/h2h?teamB=Chelsea")]
    [InlineData("/fixtures/h2h?teamA=Arsenal&teamB=%20")]
    public async Task Get_H2H_MissingTeams_ReturnsBadRequest(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Both 'teamA' and 'teamB' are required.", body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_H2H_UnknownPairing_ReturnsEmptyResult()
    {
        var response = await _client.GetAsync("/fixtures/h2h?teamA=Arsenal&teamB=Nowhere%20Rovers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal(0, body.GetProperty("count").GetInt32());
        Assert.Empty(body.GetProperty("matches").EnumerateArray());
    }
}
