namespace DAS.Domain.Common;

//Base de las entidades con identidad propia; los repositorios genéricos trabajan sobre ella
public abstract class Entity
{
    public int Id { get; set; }
}
