using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Common;

namespace DAS.Tests.Fakes;

//Confirma los repositorios en memoria que se le registran y cuenta guardados y transacciones
public sealed class FakeUnitOfWork : IUnitOfWork
{
    private readonly List<Func<int>> _commits = [];
    private readonly List<Action> _discards = [];

    public int SaveCount { get; private set; }
    public int TransactionCount { get; private set; }

    public void Track<T>(InMemoryRepository<T> repository) where T : Entity
    {
        _commits.Add(repository.Commit);
        _discards.Add(repository.Discard);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCount++;
        return Task.FromResult(_commits.Sum(commit => commit()));
    }

    public async Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default)
    {
        TransactionCount++;
        try
        {
            return await operation(cancellationToken);
        }
        catch
        {
            _discards.ForEach(discard => discard());
            throw;
        }
    }
}
