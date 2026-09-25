using LocalMock.Models;

namespace LocalMock.Services;

public interface IMockStoreRepository
{
    TResult ExecuteLocked<TResult>(Func<MockStore, Action, TResult> action);
    void ExecuteLocked(Action<MockStore, Action> action);
}

public sealed class MockStoreRepository : IMockStoreRepository
{
    private readonly string _filePath;
    private readonly ILogger<MockStoreRepository> _logger;
    private readonly object _fileLock = new();

    public MockStoreRepository(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<MockStoreRepository> logger)
    {
        _filePath = MockStorePersistence.GetFilePath(configuration, hostEnvironment);
        _logger = logger;
    }

    public TResult ExecuteLocked<TResult>(Func<MockStore, Action, TResult> action)
    {
        lock (_fileLock)
        {
            var store = MockStorePersistence.ReadStore(_filePath, _logger);
            return action(store, () => MockStorePersistence.WriteStore(_filePath, store));
        }
    }

    public void ExecuteLocked(Action<MockStore, Action> action)
    {
        lock (_fileLock)
        {
            var store = MockStorePersistence.ReadStore(_filePath, _logger);
            action(store, () => MockStorePersistence.WriteStore(_filePath, store));
        }
    }
}
