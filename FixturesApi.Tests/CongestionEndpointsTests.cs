using System.Globalization;
using System.Net;

namespace FixturesApi.Tests;

[Collection(nameof(ApiCollection))]
public class CongestionEndpointsTests(ApiFactory factory)
{
    readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_Congestion_ReturnsWindowSummaryForTeam()
    {
        var response = await _client.GetAsync("/fixtures/congestion?team=Arsenal&days=14");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal("Arsenal", body.GetProperty("team").GetString());
        Assert.Equal(14, body.GetProperty("days").GetInt32());
        Assert.True(body.GetProperty("datedMatches").GetInt32() > 0);
        Assert.True(body.GetProperty("threshold").GetInt32() >= 2);

        var windows = body.GetProperty("windows").EnumerateArray().ToList();
        Assert.Equal(windows.Count, body.GetProperty("count").GetInt32());
    }

    [Fact]
    public async Task Get_Congestion_WindowsRespectThresholdAndSpan()
    {
        var body = await (await _client.GetAsync("/fixtures/congestion?team=Arsenal&days=10")).ReadJsonAsync();

        var threshold = body.GetProperty("threshold").GetInt32();

        foreach (var w in body.GetProperty("windows").EnumerateArray())
        {
            var count = w.GetProperty("matchCount").GetInt32();
            Assert.True(count >= threshold);
            Assert.Equal(count, w.GetProperty("matches").GetArrayLength());

            var start = ParseDate(w, "start");
            var end = ParseDate(w, "end");
            var lastMatch = ParseDate(w, "lastMatchDate");

            Assert.Equal(9, end.DayNumber - start.DayNumber);
            Assert.Equal(start, ParseDate(w, "firstMatchDate"));
            Assert.InRange(lastMatch, start, end);
        }
    }

    [Fact]
    public async Task Get_Congestion_MatchesBelongToTheTeam()
    {
        var body = await (await _client.GetAsync("/fixtures/congestion?team=Liverpool&days=14")).ReadJsonAsync();

        foreach (var w in body.GetProperty("windows").EnumerateArray())
        foreach (var m in w.GetProperty("matches").EnumerateArray())
        {
            Assert.Contains("Liverpool", new[]
            {
                m.GetProperty("home").GetString(),
                m.GetProperty("away").GetString()
            });
        }
    }

    [Theory]
    [InlineData("/fixtures/congestion?days=14", "'team' is required.")]
    [InlineData("/fixtures/congestion?team=%20&days=14", "'team' is required.")]
    [InlineData("/fixtures/congestion?team=Arsenal", "'days' is required.")]
    [InlineData("/fixtures/congestion?team=Arsenal&days=0", "'days' must be between 1 and 365.")]
    [InlineData("/fixtures/congestion?team=Arsenal&days=366", "'days' must be between 1 and 365.")]
    public async Task Get_Congestion_InvalidInput_ReturnsBadRequest(string url, string expectedError)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal(expectedError, body.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Get_Congestion_UnknownTeam_ReturnsNoWindows()
    {
        var response = await _client.GetAsync("/fixtures/congestion?team=Nowhere%20Rovers&days=14");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.ReadJsonAsync();
        Assert.Equal(0, body.GetProperty("datedMatches").GetInt32());
        Assert.Equal(0, body.GetProperty("count").GetInt32());
        Assert.Empty(body.GetProperty("windows").EnumerateArray());
    }

    static DateOnly ParseDate(System.Text.Json.JsonElement element, string property) =>
        DateOnly.ParseExact(element.GetProperty(property).GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);
}
