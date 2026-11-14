namespace DAS.Application.Abstractions;

//Quién realiza la operación; la capa web lo obtiene de la sesión iniciada
public interface ICurrentUser
{
    string UserName { get; }
}
