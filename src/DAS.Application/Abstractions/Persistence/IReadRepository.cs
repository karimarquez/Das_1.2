using DAS.Domain.Common;

namespace DAS.Application.Abstractions.Persistence;

//Contrato de solo lectura para quien únicamente consulta (por ejemplo, reportes)
public interface IReadRepository<T> where T : Entity
{
    //Devuelve la entidad rastreada para que sus cambios se guarden con la unidad de trabajo
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(CancellationToken cancellationToken = default);
}
