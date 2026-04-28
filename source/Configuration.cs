using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Shoko.Abstractions.Config;

namespace Shoko.Plugin.ReleaseRetry;

/// <summary>
/// Configure the release retry behavior.
/// </summary>
[Display(Name = "Release Retry")]
public class Configuration : INewtonsoftJsonConfiguration
{
    /// <summary>
    /// The delay before retrying a failed release search.
    /// </summary>
    [Display(Name = "Retry Delay")]
    [DefaultValue(typeof(TimeSpan), "00:30:00")]
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Whether to prioritize the retry job in the queue.
    /// </summary>
    [Display(Name = "Prioritize Retry")]
    [DefaultValue(true)]
    public bool PrioritizeRetry { get; set; } = true;

    /// <summary>
    /// The maximum number of retries to attempt.
    /// </summary>
    [Display(Name = "Max Retries")]
    [Range(0, int.MaxValue)]
    [DefaultValue(1)]
    public int MaxRetries { get; set; } = 1;
}
