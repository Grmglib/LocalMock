namespace LocalMock.Models;

public sealed class ImportBatchRequest
{
    public List<ImportCollectionItem> Collections { get; set; } = new();
    public List<ImportMockItem> Mocks { get; set; } = new();
}

public sealed class ImportCollectionItem
{
    public string Id { get; set; } = string.Empty;
    public string BypassUrl { get; set; } = string.Empty;
    public bool Overwrite { get; set; }
}

public sealed class ImportMockItem
{
    public MockEntry Mock { get; set; } = new();
    public bool Overwrite { get; set; }
}

public sealed class ImportBatchResult
{
    public int CollectionsAdded { get; set; }
    public int CollectionsUpdated { get; set; }
    public int MocksAdded { get; set; }
    public int MocksUpdated { get; set; }
}
