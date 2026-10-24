using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Identity.Contracts;
using Wordrise.Modules.Identity.Core.Data;
using Wordrise.Modules.Identity.Core.Domain;
using Wordrise.Modules.Identity.Core.Email;
using Wordrise.Modules.Identity.Core.Profiles;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Database;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Identity.Core;

/// <summary>
/// Accounts, sign-in and child profiles. One identity store for every role; learners and
/// parents use the apps, staff use the admin portal with two-factor sign-in.
/// </summary>
public sealed class IdentityModuleStartup : ModuleStartup
{
    private const string AuthRateLimitPolicy = "identity-auth";

    public override string Name => "Identity";

    /// <summary>First: every other module relies on sign-in and roles.</summary>
    public override int Order => -100;

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));
        services.Configure<IdentityModuleOptions>(configuration.GetSection(IdentityModuleOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddDbContext<IdentityModuleDbContext>((provider, options) =>
            options.UseWordriseDatabase(
                provider.GetRequiredService<IOptions<DatabaseOptions>>().Value,
                IdentityModuleDbContext.Schema));

        // Bearer tokens for the mobile app, HttpOnly cookies for the website: same endpoints.
        services
            .AddIdentityApiEndpoints<AppUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                // Long and simple beats short and complex for older learners.
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<IdentityModuleDbContext>();

        services.AddOptions<IdentityOptions>().Configure<IOptions<IdentityModuleOptions>>((identity, module) =>
            identity.SignIn.RequireConfirmedEmail = module.Value.RequireConfirmedEmail);

        // Registered after Identity's own no-op sender, so these replace it.
        services.AddSingleton<IMailTransport>(provider =>
            provider.GetRequiredService<IOptions<EmailOptions>>().Value.Provider == EmailProvider.Smtp
                ? ActivatorUtilities.CreateInstance<SmtpMailTransport>(provider)
                : ActivatorUtilities.CreateInstance<LogMailTransport>(provider));
        services.AddSingleton<IEmailSender<AppUser>, AccountEmailSender>();

        services.ConfigureApplicationCookie(options =>
        {
            options.Cookie.Name = "wordrise.auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            // An API answers with status codes instead of redirecting to a login page.
            options.Events.OnRedirectToLogin = context =>
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            };
        });

        services
            .AddAuthorizationBuilder()
            .AddPolicy(Policies.Staff, policy => policy
                .RequireRole(Roles.Reviewer, Roles.Admin)
                .RequireClaim("amr", "mfa"))
            .AddPolicy(Policies.Admin, policy => policy
                .RequireRole(Roles.Admin)
                .RequireClaim("amr", "mfa"));

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy(AuthRateLimitPolicy, context =>
            {
                var limit = context.RequestServices.GetRequiredService<IOptions<IdentityModuleOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = limit.AuthRequestsPerMinute,
                        Window = TimeSpan.FromMinutes(1),
                    });
            });
        });

        services.AddScoped<ChildProfileService>();
        services.AddModuleControllers(typeof(IdentityModuleStartup).Assembly);
    }

    /// <summary>
    /// Sign-in endpoints come ready-made from ASP.NET Identity; the account and child
    /// profile endpoints are the controllers in <c>Controllers/</c>.
    /// </summary>
    public override void MapEndpoints(IEndpointRouteBuilder endpoints) =>
        endpoints
            .MapGroup("/api/v1/auth")
            .WithTags("Auth")
            .RequireRateLimiting(AuthRateLimitPolicy)
            .MapIdentityApi<AppUser>();

    public override async Task InitializeAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<IdentityModuleDbContext>();
        var databaseOptions = scope.ServiceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        await db.PrepareAsync(databaseOptions, cancellationToken);

        // Without migrations applied there are no tables to seed yet.
        if (db.Database.IsNpgsql() && !databaseOptions.ApplyMigrationsOnStartup)
        {
            return;
        }

        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach (var role in Roles.All)
        {
            if (!await roles.RoleExistsAsync(role))
            {
                await roles.CreateAsync(new IdentityRole<Guid>(role));
            }
        }

        // The owner registers normally, then is named an admin in settings; nobody can become one through the API.
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var moduleOptions = scope.ServiceProvider.GetRequiredService<IOptions<IdentityModuleOptions>>().Value;
        foreach (var email in moduleOptions.AdminEmails.Where(e => !string.IsNullOrWhiteSpace(e)).Select(e => e.Trim()))
        {
            // An address nobody has proven they own (the link was never opened) cannot be made an admin.
            if (await users.FindByEmailAsync(email) is { } admin
                && (admin.EmailConfirmed || !moduleOptions.RequireConfirmedEmail)
                && !await users.IsInRoleAsync(admin, Roles.Admin))
            {
                await users.AddToRoleAsync(admin, Roles.Admin);
            }
        }
    }
}
