using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace LocalMock.Models;

/// <summary>Entrada de mock armazenada com método, path, status e corpo da resposta.</summary>
public class MockEntry
{
    /// <summary>Identificador da coleção. Vazio/null = mock sem coleção (rota /mock/{path}).</summary>
    public string? Collection { get; set; }

    public string Method { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public int StatusCode { get; set; } = 200;
    public int ResponseDelayMs { get; set; }

    /// <summary>Content-Type da resposta mockada (ex.: application/json, application/x-www-form-urlencoded).</summary>
    public string ResponseContentType { get; set; } = "application/json";

    public JToken? ResponseBody { get; set; }

    /// <summary>Quando false em mock de coleção, a rota usa o bypass da coleção.</summary>
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue(true)]
    public bool Enabled { get; set; } = true;

    public bool BypassEnabled { get; set; }
    public string? BypassUrl { get; set; }
}
