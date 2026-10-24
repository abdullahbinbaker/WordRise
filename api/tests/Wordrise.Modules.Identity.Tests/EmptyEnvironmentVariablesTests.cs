using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Wordrise.Host;

namespace Wordrise.Modules.Identity.Tests;

/// <summary>
/// An empty line in the server's .env reaches the app as an empty environment variable. It must not
/// wipe out the value in appsettings.json.
/// </summary>
public sealed class EmptyEnvironmentVariablesTests
{
    [Fact]
    public void Empty_settings_are_removed_and_the_others_are_left_alone()
    {
        var empty = $"Wordrise_Probe_{Guid.NewGuid():N}__Empty";
        var blank = $"Wordrise_Probe_{Guid.NewGuid():N}__Blank";
        var filled = $"Wordrise_Probe_{Guid.NewGuid():N}__Filled";
        var notASetting = $"WORDRISE_PROBE_{Guid.NewGuid():N}";
        try
        {
            Environment.SetEnvironmentVariable(empty, "");
            Environment.SetEnvironmentVariable(blank, "   ");
            Environment.SetEnvironmentVariable(filled, "value");
            Environment.SetEnvironmentVariable(notASetting, "");

            var removed = EmptyEnvironmentVariables.Ignore();

            Assert.Contains(empty, removed);
            Assert.Contains(blank, removed);
            Assert.DoesNotContain(filled, removed);
            Assert.DoesNotContain(notASetting, removed);
            Assert.Null(Environment.GetEnvironmentVariable(empty));
            Assert.Equal("value", Environment.GetEnvironmentVariable(filled));
        }
        finally
        {
            foreach (var name in new[] { empty, blank, filled, notASetting })
            {
                Environment.SetEnvironmentVariable(name, null);
            }
        }
    }

    [Fact]
    public void An_empty_variable_no_longer_wipes_out_the_value_in_appsettings()
    {
        // appsettings.json says Authoring:MaxCycles is 3; an empty variable used to turn it into nothing.
        Environment.SetEnvironmentVariable("Authoring__MaxCycles", "");
        try
        {
            using var factory = new WordriseApiFactory();

            var value = factory.Services.GetRequiredService<IConfiguration>()["Authoring:MaxCycles"];

            Assert.Equal("3", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Authoring__MaxCycles", null);
        }
    }

    [Fact]
    public void A_filled_variable_still_wins_over_appsettings()
    {
        Environment.SetEnvironmentVariable("Authoring__MaxCycles", "2");
        try
        {
            using var factory = new WordriseApiFactory();

            var value = factory.Services.GetRequiredService<IConfiguration>()["Authoring:MaxCycles"];

            Assert.Equal("2", value);
        }
        finally
        {
            Environment.SetEnvironmentVariable("Authoring__MaxCycles", null);
        }
    }
}
