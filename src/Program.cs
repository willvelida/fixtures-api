using FixturesApi.Data;
using FixturesApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<FixtureData>();

var app = builder.Build();

// Reflection-scans every IEndpointModule, so adding an endpoint never touches this file.
app.MapEndpointModules();

app.Run();
public partial class Program;

public partial class Program;
