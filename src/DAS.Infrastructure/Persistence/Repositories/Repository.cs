using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence.Repositories;

//Operaciones comunes a todas las entidades; los repositorios concretos solo agregan sus consultas
public class Repository<T>(AppDbContext context) : IRepository<T> where T : Entity
{
    protected AppDbContext Context { get; } = context;

    protected DbSet<T> Set => Context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public virtual async Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().ToListAsync(cancellationToken);

    public void Add(T entity) => Set.Add(entity);

    public void Update(T entity) => Set.Update(entity);

    public void Remove(T entity) => Set.Remove(entity);
}
