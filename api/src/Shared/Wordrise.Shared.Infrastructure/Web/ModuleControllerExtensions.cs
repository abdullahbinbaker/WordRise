using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.DependencyInjection;

namespace Wordrise.Shared.Infrastructure.Web;

public static class ModuleControllerExtensions
{
    /// <summary>
    /// Exposes the controllers of a module assembly. Module controllers stay <c>internal</c>
    /// so nothing outside the module can use them directly.
    /// </summary>
    public static IServiceCollection AddModuleControllers(this IServiceCollection services, Assembly moduleAssembly)
    {
        services
            .AddControllers()
            .AddJsonOptions(options =>
            {
                // Enums travel as readable strings ("good", "mastered") instead of numbers.
                if (!options.JsonSerializerOptions.Converters.OfType<JsonStringEnumConverter>().Any())
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                }
            })
            .AddApplicationPart(moduleAssembly)
            .ConfigureApplicationPartManager(manager =>
            {
                if (manager.FeatureProviders.OfType<ModuleControllerFeatureProvider>().Any())
                {
                    return;
                }

                // Replace MVC's default provider, which only accepts public controllers.
                foreach (var provider in manager.FeatureProviders.OfType<ControllerFeatureProvider>().ToList())
                {
                    manager.FeatureProviders.Remove(provider);
                }

                manager.FeatureProviders.Add(new ModuleControllerFeatureProvider());
            });

        return services;
    }

    private sealed class ModuleControllerFeatureProvider : ControllerFeatureProvider
    {
        protected override bool IsController(TypeInfo typeInfo) =>
            typeInfo.IsClass
            && !typeInfo.IsAbstract
            && !typeInfo.ContainsGenericParameters
            && !typeInfo.IsDefined(typeof(NonControllerAttribute))
            && typeof(ControllerBase).IsAssignableFrom(typeInfo);
    }
}
