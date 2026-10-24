using Mediator;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Wordrise.Host;
using Wordrise.Modules.ContentAuthoring.Api;
using Wordrise.Modules.Identity.Core;
using Wordrise.Modules.Learning.Api;
using Wordrise.Modules.Media.Core;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Web;

// An empty setting in the environment means "not set" (see the class), so appsettings.json can supply it.
EmptyEnvironmentVariables.Ignore();

var builder = WebApplication.CreateBuilder(args);

// Every module of the monolith, listed explicitly so nothing starts by accident.
IReadOnlyList<IModuleStartup> modules =
[
    .. new IModuleStartup[]
    {
        new IdentityModuleStartup(),
        new LearningModuleStartup(),
        new ContentAuthoringModuleStartup(),
        new MediaModuleStartup(),
    }.OrderBy(module => module.Order),
];

// Behind a reverse proxy (Caddy) the app must believe the proxy about who is calling and over what:
// the rate limiter counts by client address and the proxy terminates HTTPS. Off by default, because
// without a proxy in front any caller could forge these headers.
var trustProxyHeaders = builder.Configuration.GetValue<bool>("Hosting:TrustProxyHeaders");
if (trustProxyHeaders)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });
}

// Sign-in cookies and tokens are encrypted with these keys: kept on a volume, a restart or a new
// container does not sign everybody out.
var dataProtectionKeys = builder.Configuration["DataProtection:KeysPath"];
if (!string.IsNullOrWhiteSpace(dataProtectionKeys))
{
    builder.Services.AddDataProtection()
        .SetApplicationName("Wordrise")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeys));
}

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<BusinessRuleExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [])
    .AllowCredentials()
    .AllowAnyHeader()
    .AllowAnyMethod()));

// Commands and queries of all modules; handlers use scoped services such as DbContexts.
builder.Services.AddMediator((MediatorOptions options) =>
{
    options.Namespace = "Wordrise.Host.Mediator";
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

builder.ConfigureModules(modules);

var app = builder.Build();

if (trustProxyHeaders)
{
    app.UseForwardedHeaders();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/health");
app.MapControllers();
app.MapModules(modules);

await app.InitializeModulesAsync(modules);
await app.RunAsync();

/// <summary>Entry point, public so integration tests can host the app.</summary>
public partial class Program;
