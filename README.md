# Shoko Release Retry Plugin

A [Shoko](https://shokoanime.com/) plugin that re-attempts automatic release searches after a configurable delay when the initial auto-match attempt fails.

## Features

- **Automatic Retry Scheduling** — If an auto-match release search fails, the plugin schedules a retry after a configurable delay.
- **Configurable Retry Count** — Limit how many times a video will be retried.
- **Prioritize Retries** — Optionally prioritize retry jobs in the processing queue.
- **Cancellation on Deletion** — Pending retries are automatically cancelled if the associated video file is deleted.

## Installation

### GUI (Recommended)

1. Open the Shoko Web UI and navigate to **Settings → Plugins → Repositories**.
2. Add the manifest URL:
   ```
   https://raw.githubusercontent.com/revam/dotnet-shoko-plugin-release-retry/stable/manifest.json
   ```
3. Go to **Server → Plugins → Browse** and find **Release Retry**.
4. Click **Install** on the desired version.
5. Restart Shoko.

### Manual

1. Download the latest `Shoko.Plugin.ReleaseRetry-<version>-any.zip` from the [Releases](../../releases) page.
2. Extract the ZIP and place `Shoko.Plugin.ReleaseRetry.dll` into your Shoko **Plugins** folder.
3. Restart Shoko.

## Configuration

The plugin exposes the following settings in the Shoko UI:

| Setting | Default | Description |
|---|---|---|
| **Retry Delay** | `00:30:00` (30 minutes) | How long to wait before retrying a failed release search. |
| **Prioritize Retry** | `true` | Whether to prioritize the retry job in the queue. |
| **Max Retries** | `1` | The maximum number of retry attempts per video. Set to `0` to disable retries. |

## Building from Source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet restore
dotnet build --configuration Release
```

The compiled assembly will be located at `bin/Release/net10.0/Shoko.Plugin.ReleaseRetry.dll`.

## License

This project is licensed under the MIT License.
