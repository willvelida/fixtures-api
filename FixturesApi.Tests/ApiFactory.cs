using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FixturesApi.Tests;

// FixtureData reads seed/fixtures.json relative to the content root, so the factory pins the
// content root to the API project directory rather than the test assembly's output folder.
public class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseContentRoot(ResolveApiContentRoot());

    static string ResolveApiContentRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);

        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "src");
            if (File.Exists(Path.Combine(candidate, "seed", "fixtures.json")))
                return candidate;

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate the API content root containing seed/fixtures.json.");
    }
}

[CollectionDefinition(nameof(ApiCollection))]
public class ApiCollection : ICollectionFixture<ApiFactory>;

public static class JsonHelpers
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static async Task<JsonElement> ReadJsonAsync(this HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.Clone();
    }

    public static async Task<T> ReadAsAsync<T>(this HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Options);
        Assert.NotNull(value);
        return value!;
    }
}
