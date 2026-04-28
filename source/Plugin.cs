using System;
using Microsoft.Extensions.DependencyInjection;
using Shoko.Abstractions.Plugin;
using Shoko.Abstractions.Utilities;

namespace Shoko.Plugin.ReleaseRetry;

/// <summary>
/// Plugin responsible for re-attempting release searches after a configurable
/// delay if the first auto-match attempt fails.
/// </summary>
public class Plugin : IPlugin, IPluginServiceRegistration
{
    /// <inheritdoc/>
    public Guid ID { get; private init; } = new("b81acd7e-6641-47e7-a383-0a8b4304408e");

    /// <inheritdoc/>
    public string Name { get; private set; } = "Release Retry";

    /// <inheritdoc/>
    public string Description { get; private set; } = """
        Re-attempts release searches after a delay if the first auto-match attempt fails.
    """;

    /// <inheritdoc/>
    public static void RegisterServices(IServiceCollection serviceCollection, IApplicationPaths applicationPaths)
    {
        serviceCollection.AddSingleton<ReleaseRetryService>();
        serviceCollection.AddHostedService(sp => sp.GetRequiredService<ReleaseRetryService>());
    }
}
