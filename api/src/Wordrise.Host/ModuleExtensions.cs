using Wordrise.Shared.Abstractions;

namespace Wordrise.Host;

internal static class ModuleExtensions
{
    public static void ConfigureModules(this WebApplicationBuilder builder, IReadOnlyList<IModuleStartup> modules)
    {
        foreach (var module in modules)
        {
            module.ConfigureServices(builder.Services, builder.Configuration);
        }
    }

    public static void MapModules(this WebApplication app, IReadOnlyList<IModuleStartup> modules)
    {
        foreach (var module in modules)
        {
            module.MapEndpoints(app);
        }
    }

    public static async Task InitializeModulesAsync(this WebApplication app, IReadOnlyList<IModuleStartup> modules)
    {
        foreach (var module in modules)
        {
            app.Logger.LogInformation("Initializing module {Module}", module.Name);
            await module.InitializeAsync(app.Services, app.Lifetime.ApplicationStopping);
        }
    }
}
