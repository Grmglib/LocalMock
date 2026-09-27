using System.Diagnostics;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace LocalMock.Services;

public interface IAppUpdateService
{
    Task<VersionStatusResult> GetVersionStatusAsync(CancellationToken cancellationToken = default);

    Task<UpdateStartResult> StartUpdateAsync(CancellationToken cancellationToken = default);
}

public sealed class VersionStatusResult
{
    public string CurrentVersion { get; init; } = AppVersion.Current;

    public string? LatestVersion { get; init; }

    public bool UpdateAvailable { get; init; }

    public string? ReleaseUrl { get; init; }

    public string? ReleaseNotes { get; init; }

    public string? Error { get; init; }
}

public sealed class UpdateStartResult
{
    public bool Started { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? TargetVersion { get; init; }

    public int StatusCode { get; init; } = 200;
}

public sealed class AppUpdateService : IAppUpdateService
{
    private readonly IGitHubReleaseService _gitHubReleaseService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateOptions _options;
    private readonly ILogger<AppUpdateService> _logger;
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _updateLock = new(1, 1);
    private bool _updateInProgress;

    public AppUpdateService(
        IGitHubReleaseService gitHubReleaseService,
        IHttpClientFactory httpClientFactory,
        IOptions<UpdateOptions> options,
        ILogger<AppUpdateService> logger,
        IConfiguration configuration)
    {
        _gitHubReleaseService = gitHubReleaseService;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<VersionStatusResult> GetVersionStatusAsync(CancellationToken cancellationToken = default)
    {
        var current = AppVersion.Current;

        try
        {
            var latest = await _gitHubReleaseService.GetLatestReleaseAsync(cancellationToken);
            if (latest == null)
            {
                return new VersionStatusResult
                {
                    CurrentVersion = current,
                    LatestVersion = null,
                    UpdateAvailable = false
                };
            }

            var latestVersion = AppVersion.Normalize(latest.TagName);
            var newer = AppVersion.IsNewer(latestVersion, current);
            var hasAsset = !string.IsNullOrWhiteSpace(latest.AssetDownloadUrl);
            return new VersionStatusResult
            {
                CurrentVersion = current,
                LatestVersion = latestVersion,
                UpdateAvailable = newer && hasAsset,
                ReleaseUrl = latest.HtmlUrl,
                ReleaseNotes = latest.Body,
                Error = newer && !hasAsset
                    ? $"Release {latestVersion} does not contain '{_options.AssetName}'."
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check GitHub releases for updates.");
            return new VersionStatusResult
            {
                CurrentVersion = current,
                UpdateAvailable = false,
                Error = "Unable to check for updates right now."
            };
        }
    }

    public async Task<UpdateStartResult> StartUpdateAsync(CancellationToken cancellationToken = default)
    {
        if (!await _updateLock.WaitAsync(0, cancellationToken))
        {
            return new UpdateStartResult
            {
                Started = false,
                StatusCode = 409,
                Message = "An update is already in progress."
            };
        }

        try
        {
            if (_updateInProgress)
            {
                return new UpdateStartResult
                {
                    Started = false,
                    StatusCode = 409,
                    Message = "An update is already in progress."
                };
            }

            var release = await _gitHubReleaseService.GetLatestReleaseAsync(cancellationToken);
            if (release == null || !AppVersion.IsNewer(release.TagName, AppVersion.Current))
            {
                return new UpdateStartResult
                {
                    Started = false,
                    StatusCode = 400,
                    Message = "No update available."
                };
            }

            if (string.IsNullOrWhiteSpace(release.AssetDownloadUrl))
            {
                return new UpdateStartResult
                {
                    Started = false,
                    StatusCode = 404,
                    Message = $"Asset '{_options.AssetName}' was not found in the latest release."
                };
            }

            var zipPath = Path.Combine(
                Path.GetTempPath(),
                $"LocalMock-update-{Guid.NewGuid():N}.zip");

            _logger.LogInformation(
                "Downloading update {Version} from {Url}",
                release.TagName,
                release.AssetDownloadUrl);

            try
            {
                await DownloadAssetAsync(release.AssetDownloadUrl, zipPath, cancellationToken);
            }
            catch
            {
                File.Delete(zipPath);
                throw;
            }

            var helperPath = ResolveApplyUpdateScriptPath();
            if (helperPath == null)
            {
                File.Delete(zipPath);
                return new UpdateStartResult
                {
                    Started = false,
                    StatusCode = 500,
                    Message = "Script apply-update.ps1 was not found in the installation."
                };
            }

            var installDir = AppContext.BaseDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

            var port = _configuration.GetValue<int>("LocalMock:Port", 5183);
            LaunchUpdater(helperPath, zipPath, installDir, $"http://localhost:{port}/ui/");
            _updateInProgress = true;

            return new UpdateStartResult
            {
                Started = true,
                StatusCode = 202,
                Message = "Update started. LocalMock will restart shortly.",
                TargetVersion = AppVersion.Normalize(release.TagName)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start application update.");
            return new UpdateStartResult
            {
                Started = false,
                StatusCode = 500,
                Message = "Failed to start the update: " + ex.Message
            };
        }
        finally
        {
            _updateLock.Release();
        }
    }

    private async Task DownloadAssetAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient(nameof(GitHubReleaseService));
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd("LocalMock-Updater");

        // API asset URLs require octet-stream; browser_download_url works better with */* / empty Accept.
        var isApiAssetUrl = url.Contains("api.github.com", StringComparison.OrdinalIgnoreCase)
            && url.Contains("/releases/assets/", StringComparison.OrdinalIgnoreCase);
        if (isApiAssetUrl)
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
        }
        else
        {
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }

        if (!string.IsNullOrWhiteSpace(_options.GitHubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.GitHubToken);
        }

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var remote = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var local = File.Create(destinationPath);
        await remote.CopyToAsync(local, cancellationToken);
    }

    private static string? ResolveApplyUpdateScriptPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "scripts", "apply-update.ps1"),
            Path.Combine(AppContext.BaseDirectory, "apply-update.ps1")
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void LaunchUpdater(string scriptPath, string zipPath, string installDir, string healthUrl)
    {
        var temporaryScript = Path.Combine(Path.GetTempPath(), $"LocalMock-updater-{Guid.NewGuid():N}.ps1");
        File.Copy(scriptPath, temporaryScript);

        var startInfo = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath()
        };

        foreach (var argument in new[]
        {
            "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", temporaryScript,
            "-ZipPath", zipPath, "-InstallDir", installDir,
            "-AppProcessId", Environment.ProcessId.ToString(), "-HealthUrl", healthUrl
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        _logger.LogInformation("Launching update helper for process {ProcessId}", Environment.ProcessId);

        try
        {
            if (Process.Start(startInfo) == null)
            {
                throw new InvalidOperationException("Unable to start the update helper.");
            }
        }
        catch
        {
            File.Delete(temporaryScript);
            File.Delete(zipPath);
            throw;
        }
    }
}
