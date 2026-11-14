using DAS.Domain.Common;

namespace DAS.Application.Abstractions.Persistence;

//Los cambios no se guardan aquí: se confirman con IUnitOfWork.SaveChangesAsync
public interface IRepository<T> : IReadRepository<T> where T : Entity
{
    void Add(T entity);

    //Solo para entidades que no vienen de este repositorio; las obtenidas con GetByIdAsync ya se rastrean
    void Update(T entity);

    void Remove(T entity);
}
