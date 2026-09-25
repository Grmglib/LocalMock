using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace LocalMock.Models;

/// <summary>Stored mock entry with method, path, status, and response body.</summary>
public class MockEntry
{
    /// <summary>Collection identifier. Empty/null = standalone mock (route /mock/{path}).</summary>
    public string? Collection { get; set; }

    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public int ResponseDelayMs { get; set; }

    /// <summary>Content-Type of the mocked response (e.g. application/json, application/x-www-form-urlencoded).</summary>
    public string ResponseContentType { get; set; } = "application/json";

    public JToken? ResponseBody { get; set; }

    /// <summary>When false for a collection mock, the route uses the collection bypass.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    public bool BypassEnabled { get; set; }
    public string? BypassUrl { get; set; }
}
