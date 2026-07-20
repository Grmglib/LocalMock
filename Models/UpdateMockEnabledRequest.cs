namespace LocalMock.Models;

/// <summary>Request para ativar ou desativar um mock de coleção.</summary>
public class UpdateMockEnabledRequest
{
    /// <summary>Identificador da coleção.</summary>
    public string Collection { get; set; } = string.Empty;

    /// <summary>Método HTTP do mock.</summary>
    public string Method { get; set; } = "GET";

    /// <summary>Path do endpoint mockado.</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Indica se o mock está ativo na coleção.</summary>
    public bool Enabled { get; set; } = true;
}
