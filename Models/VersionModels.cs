namespace LocalMock.Models;

public class VersionStatusResponse
{
    public string CurrentVersion { get; set; } = "0.0.0";

    public string? LatestVersion { get; set; }

    public bool UpdateAvailable { get; set; }

    public string? ReleaseUrl { get; set; }

    public string? ReleaseNotes { get; set; }

    public string? Error { get; set; }
}

public class UpdateStartResponse
{
    public bool Started { get; set; }

    public string Message { get; set; } = string.Empty;

    public string? TargetVersion { get; set; }
}
