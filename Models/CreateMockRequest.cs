using Newtonsoft.Json.Linq;

namespace LocalMock.Models;

/// <summary>Request para criação de um mock de endpoint.</summary>
public class CreateMockRequest
{
    /// <summary>Identificador da coleção. Opcional para mocks sem coleção.</summary>
    public string? Collection { get; set; }

    /// <summary>Método HTTP (GET, POST, PUT, DELETE, PATCH).</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Path do endpoint (ex.: /ConsultarCliente).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Código de status HTTP da resposta.</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>Tempo de espera antes de retornar a resposta mockada, em milissegundos.</summary>
    public int ResponseDelayMs { get; set; }

    /// <summary>Content-Type da resposta mockada.</summary>
    public string? ResponseContentType { get; set; }

    /// <summary>Corpo da resposta (objeto JSON, string form-urlencoded, texto, etc.).</summary>
    public object? ResponseBody { get; set; }

    /// <summary>Indica se o mock está ativo (coleções). Padrão: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Indica se a chamada deve ser encaminhada para uma URL externa.</summary>
    public bool BypassEnabled { get; set; }

    /// <summary>URL absoluta que receberá a chamada quando o bypass estiver ativo.</summary>
    public string? BypassUrl { get; set; }
}
