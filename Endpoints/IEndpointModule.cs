using System.Reflection;

namespace FixturesApi.Endpoints;

// Each endpoint file implements this; adding one is a new file, never a change to Program.cs.
public interface IEndpointModule
{
    void MapEndpoints(IEndpointRouteBuilder app);
}

public static class EndpointModuleExtensions
{
    public static void MapEndpointModules(this IEndpointRouteBuilder app)
    {
        var modules = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => t is { IsInterface: false, IsAbstract: false }
                        && typeof(IEndpointModule).IsAssignableFrom(t))
            .Select(t => (IEndpointModule)Activator.CreateInstance(t)!);

        foreach (var module in modules)
            module.MapEndpoints(app);
    }
}
