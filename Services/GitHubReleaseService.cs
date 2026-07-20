using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace LocalMock.Services;

public interface IGitHubReleaseService
{
    Task<GitHubLatestRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default);
}

public sealed class GitHubLatestRelease
{
    public string TagName { get; init; } = string.Empty;

    public string? HtmlUrl { get; init; }

    public string? Body { get; init; }

    /// <summary>API URL do asset (releases/assets/{id}) — preferida para download autenticado.</summary>
    public string? AssetDownloadUrl { get; init; }

    public string? AssetName { get; init; }
}

public sealed class GitHubReleaseService : IGitHubReleaseService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateOptions _options;
    private readonly ILogger<GitHubReleaseService> _logger;
    private readonly object _cacheLock = new();
    private GitHubLatestRelease? _cachedRelease;
    private DateTimeOffset _cacheExpiresAt = DateTimeOffset.MinValue;

    public GitHubReleaseService(
        IHttpClientFactory httpClientFactory,
        IOptions<UpdateOptions> options,
        ILogger<GitHubReleaseService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<GitHubLatestRelease?> GetLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        lock (_cacheLock)
        {
            if (_cachedRelease != null && DateTimeOffset.UtcNow < _cacheExpiresAt)
            {
                return _cachedRelease;
            }
        }

        if (string.IsNullOrWhiteSpace(_options.GitHubOwner) || string.IsNullOrWhiteSpace(_options.GitHubRepo))
        {
            _logger.LogWarning("GitHub owner/repo not configured for updates.");
            return null;
        }

        var client = _httpClientFactory.CreateClient(nameof(GitHubReleaseService));
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"https://api.github.com/repos/{_options.GitHubOwner}/{_options.GitHubRepo}/releases/latest");

        request.Headers.UserAgent.ParseAdd("LocalMock-Updater");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        if (!string.IsNullOrWhiteSpace(_options.GitHubToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.GitHubToken);
        }

        using var response = await client.SendAsync(request, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogInformation("No GitHub releases found for {Owner}/{Repo}.", _options.GitHubOwner, _options.GitHubRepo);
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<GitHubReleaseApiResponse>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
            cancellationToken);

        if (payload == null || string.IsNullOrWhiteSpace(payload.TagName))
        {
            return null;
        }

        var asset = payload.Assets?
            .FirstOrDefault(item =>
                string.Equals(item.Name, _options.AssetName, StringComparison.OrdinalIgnoreCase));

        var release = new GitHubLatestRelease
        {
            TagName = payload.TagName,
            HtmlUrl = payload.HtmlUrl,
            Body = payload.Body,
            AssetName = asset?.Name,
            // Prefer API asset URL so authenticated downloads work (private repos / fine-grained PATs).
            AssetDownloadUrl = !string.IsNullOrWhiteSpace(asset?.Url)
                ? asset.Url
                : asset?.BrowserDownloadUrl
        };

        var cacheSeconds = Math.Max(60, _options.CheckIntervalSeconds);
        lock (_cacheLock)
        {
            _cachedRelease = release;
            _cacheExpiresAt = DateTimeOffset.UtcNow.AddSeconds(cacheSeconds);
        }

        return release;
    }

    private sealed class GitHubReleaseApiResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubAssetApiResponse>? Assets { get; set; }
    }

    private sealed class GitHubAssetApiResponse
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
