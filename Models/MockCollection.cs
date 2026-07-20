namespace LocalMock.Models;

/// <summary>Coleção de mocks com URL de bypass para endpoints não configurados.</summary>
public class MockCollection
{
    /// <summary>Identificador da coleção (slug, ex.: parceiro).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>URL base absoluta HTTP/HTTPS para encaminhamento quando não houver mock.</summary>
    public string BypassUrl { get; set; } = string.Empty;
}
