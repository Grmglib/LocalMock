namespace LocalMock.Models;

/// <summary>Envelope persistido em mocks.json com coleções e mocks.</summary>
public class MockStore
{
    public List<MockCollection> Collections { get; set; } = new();
    public List<MockEntry> Mocks { get; set; } = new();
}
