using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wordrise.Shared.Abstractions;

/// <summary>
/// The entry point of a business module in the modular monolith. The host only knows modules
/// through this contract: each module registers its own services, maps its own endpoints and
/// prepares its own data. Derive from <see cref="ModuleStartup"/> rather than implementing it.
/// </summary>
public interface IModuleStartup
{
    string Name { get; }

    /// <summary>Modules start in ascending order; lower runs first.</summary>
    int Order { get; }

    void ConfigureServices(IServiceCollection services, IConfiguration configuration);

    void MapEndpoints(IEndpointRouteBuilder endpoints);

    /// <summary>Runs once at startup, before the app accepts requests.</summary>
    Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken);
}
