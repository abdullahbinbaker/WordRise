using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Modules.ContentAuthoring.Api.Services;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;
using Wordrise.Modules.ContentAuthoring.Infrastructure.Extensions;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.ContentAuthoring.Api;

/// <summary>
/// Produces the lessons: the AI writes and reviews them in the background, a person reads them
/// and publishes them to the Learning module. Built in layers:
/// Domain (rules) ← Application (commands and the pipeline) ← Infrastructure (database, Claude, worker) ← Api (controllers).
/// </summary>
public sealed class ContentAuthoringModuleStartup : ModuleStartup
{
    public override string Name => "ContentAuthoring";

    /// <summary>After Identity (staff sign-in) and Learning (where lessons are published).</summary>
    public override int Order => 10;

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddAuthoringInfrastructure(configuration);
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentStaff, HttpCurrentStaff>();
        services.AddModuleControllers(typeof(ContentAuthoringModuleStartup).Assembly);
    }

    public override Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken) =>
        services.InitializeAuthoringDatabaseAsync(cancellationToken);
}
