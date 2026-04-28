using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shoko.Abstractions.Config;
using Shoko.Abstractions.Video;
using Shoko.Abstractions.Video.Events;
using Shoko.Abstractions.Video.Services;

namespace Shoko.Plugin.ReleaseRetry;

/// <summary>
/// Hosted service that schedules retries for failed automatic release searches.
/// </summary>
public class ReleaseRetryService : IHostedService
{
    private readonly ILogger<ReleaseRetryService> _logger;
    private readonly IVideoReleaseService _videoReleaseService;
    private readonly IVideoService _videoService;
    private readonly ConfigurationProvider<Configuration> _configProvider;

    private readonly ConcurrentDictionary<int, CancellationTokenSource> _pendingRetries = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ReleaseRetryService"/> class.
    /// </summary>
    /// <param name="logger">The logger.</param>
    /// <param name="videoReleaseService">The video release service.</param>
    /// <param name="videoService">The video service.</param>
    /// <param name="configProvider">The configuration provider.</param>
    public ReleaseRetryService(
        ILogger<ReleaseRetryService> logger,
        IVideoReleaseService videoReleaseService,
        IVideoService videoService,
        ConfigurationProvider<Configuration> configProvider)
    {
        _logger = logger;
        _videoReleaseService = videoReleaseService;
        _videoService = videoService;
        _configProvider = configProvider;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _videoReleaseService.SearchCompleted += OnSearchCompleted;
        _videoService.VideoFileDeleted += OnVideoFileDeleted;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken)
    {
        _videoReleaseService.SearchCompleted -= OnSearchCompleted;
        _videoService.VideoFileDeleted -= OnVideoFileDeleted;

        foreach (var pair in _pendingRetries)
        {
            try
            {
                pair.Value.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }
        }

        _pendingRetries.Clear();
        return Task.CompletedTask;
    }

    private void OnSearchCompleted(object? sender, VideoReleaseSearchCompletedEventArgs eventArgs)
    {
        if (!eventArgs.IsAutomatic)
            return;

        if (eventArgs.IsSuccessful)
            return;

        var config = _configProvider.Load();
        if (config.MaxRetries <= 0)
            return;

        var video = eventArgs.Video;
        var attempts = _videoReleaseService.GetReleaseMatchAttemptsForVideo(video);
        if (attempts.Count > config.MaxRetries)
            return;

        var currentRelease = _videoReleaseService.GetCurrentReleaseForVideo(video);
        if (currentRelease is not null)
            return;

        // Cancel any existing pending retry for this video before scheduling a new one.
        if (_pendingRetries.TryRemove(video.ID, out var existingCts))
        {
            try
            {
                existingCts.Cancel();
                existingCts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }
        }

        var cts = new CancellationTokenSource();
        if (!_pendingRetries.TryAdd(video.ID, cts))
        {
            cts.Dispose();
            return;
        }

        _logger.LogInformation(
            "Scheduling retry #{RetryNumber} for video {VideoID} in {Delay} because the auto-match attempt failed.",
            attempts.Count,
            video.ID,
            config.RetryDelay
        );

        _ = RunRetryAsync(video, config.RetryDelay, config.PrioritizeRetry, cts.Token);
    }

    private void OnVideoFileDeleted(object? sender, VideoFileEventArgs eventArgs)
    {
        if (_videoService.GetVideoByID(eventArgs.Video.ID) is not null)
            return;

        if (_pendingRetries.TryRemove(eventArgs.Video.ID, out var cts))
        {
            try
            {
                cts.Cancel();
                cts.Dispose();
            }
            catch (ObjectDisposedException)
            {
                // Ignore.
            }

            _logger.LogDebug("Cancelled pending retry for video {VideoID} because the video was deleted.", eventArgs.Video.ID);
        }
    }

    private async Task RunRetryAsync(IVideo video, TimeSpan delay, bool prioritize, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
            return;

        _pendingRetries.TryRemove(video.ID, out _);

        var currentVideo = _videoService.GetVideoByID(video.ID);
        if (currentVideo is null)
        {
            _logger.LogDebug("Skipping retry for video {VideoID} because it no longer exists.", video.ID);
            return;
        }

        var release = _videoReleaseService.GetCurrentReleaseForVideo(currentVideo);
        if (release is not null)
        {
            _logger.LogDebug("Skipping retry for video {VideoID} because it already has a release.", video.ID);
            return;
        }

        _logger.LogInformation("Retrying release search for video {VideoID}.", video.ID);
        await _videoReleaseService.ScheduleFindReleaseForVideo(currentVideo, force: true, prioritize: prioritize).ConfigureAwait(false);
    }
}
