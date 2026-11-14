namespace DAS.Domain.Exceptions;

//Una regla del negocio fue violada; el mensaje se puede mostrar al usuario
public class DomainException(string message) : Exception(message)
{
}
