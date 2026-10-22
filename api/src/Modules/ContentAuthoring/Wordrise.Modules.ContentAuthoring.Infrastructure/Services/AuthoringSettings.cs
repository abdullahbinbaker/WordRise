using Microsoft.Extensions.Options;
using Wordrise.Modules.ContentAuthoring.Application.Abstractions;

namespace Wordrise.Modules.ContentAuthoring.Infrastructure.Services;

internal sealed class AuthoringSettings(IOptions<AuthoringOptions> options) : IAuthoringSettings
{
    public int MaxCycles => options.Value.MaxCycles;
}
