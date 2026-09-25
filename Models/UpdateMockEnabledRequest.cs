namespace LocalMock.Models;

/// <summary>Request to enable or disable a collection mock.</summary>
public class UpdateMockEnabledRequest
{
    /// <summary>Collection identifier.</summary>
    public string Collection { get; set; } = string.Empty;

    /// <summary>HTTP method of the mock.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Path of the mocked endpoint.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Whether the mock is active in the collection.</summary>
    public bool Enabled { get; set; } = true;
}
