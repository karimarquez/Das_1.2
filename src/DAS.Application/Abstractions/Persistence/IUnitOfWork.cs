namespace DAS.Application.Abstractions.Persistence;

//Todos los repositorios de una petición comparten la unidad de trabajo: se confirman juntos o no se confirma nada
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    //Si la operación lanza una excepción se revierte todo lo guardado dentro de ella
    Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default);
}
