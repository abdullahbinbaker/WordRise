using Mediator;
using Wordrise.Host;
using Wordrise.Modules.ContentAuthoring.Api;
using Wordrise.Modules.Identity.Core;
using Wordrise.Modules.Learning.Api;
using Wordrise.Modules.Media.Core;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Web;

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
