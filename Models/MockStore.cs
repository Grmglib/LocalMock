namespace LocalMock.Models;

/// <summary>Persisted envelope in mocks.json with collections and mocks.</summary>
public class MockStore
{
    public List<MockCollection> Collections { get; set; } = new();
    public List<MockEntry> Mocks { get; set; } = new();
}
