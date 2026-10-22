using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Wordrise.Modules.Media.Contracts;
using Wordrise.Modules.Media.Core.Audio;
using Wordrise.Shared.Abstractions;
using Wordrise.Shared.Infrastructure.Web;

namespace Wordrise.Modules.Media.Core;

/// <summary>
/// Recordings of the English texts learners listen to: made once when a lesson is published,
/// kept in a folder, served to the apps. Simple enough for one layer.
/// </summary>
public sealed class MediaModuleStartup : ModuleStartup
{
    public override string Name => "Media";

    public override void ConfigureServices(IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<MediaOptions>()
            .Bind(configuration.GetSection(MediaOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<MediaOptions>, MediaOptionsValidator>());

        services.AddSingleton<IAudioStore, FolderAudioStore>();
        services.AddHttpClient<ISpeechSynthesizer, AzureSpeechSynthesizer>(client => client.Timeout = TimeSpan.FromSeconds(30));
        services.AddScoped<IAudioLibrary, AudioLibrary>();
        services.AddModuleControllers(typeof(MediaModuleStartup).Assembly);
    }
}
