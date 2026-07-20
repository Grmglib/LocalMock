namespace LocalMock.Models;

/// <summary>Request para ativar ou desativar o bypass de um mock salvo.</summary>
public class UpdateBypassRequest
{
    /// <summary>Identificador da coleção. Opcional para mocks sem coleção.</summary>
    public string? Collection { get; set; }

    /// <summary>Método HTTP do mock.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Path do endpoint mockado.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Indica se a chamada deve ser encaminhada para a URL de bypass.</summary>
    public bool BypassEnabled { get; set; }

    /// <summary>URL absoluta que receberá a chamada quando o bypass estiver ativo.</summary>
    public string? BypassUrl { get; set; }
}
