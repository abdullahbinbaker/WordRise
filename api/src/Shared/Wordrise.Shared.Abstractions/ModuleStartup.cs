using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Wordrise.Shared.Abstractions;

/// <summary>Base class for module entry points: override only what the module needs.</summary>
public abstract class ModuleStartup : IModuleStartup
{
    public abstract string Name { get; }

    public virtual int Order => 0;

    public virtual void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
    }

    public virtual void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
    }

    public virtual Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
