namespace LocalMock.Services;

public class UpdateOptions
{
    public const string SectionName = "LocalMock:Updates";

    public string GitHubOwner { get; set; } = "Grmglib";

    public string GitHubRepo { get; set; } = "LocalMock";

    public string AssetName { get; set; } = "LocalMock-win-x64.zip";

    public int CheckIntervalSeconds { get; set; } = 3600;

    public string? GitHubToken { get; set; }
}
