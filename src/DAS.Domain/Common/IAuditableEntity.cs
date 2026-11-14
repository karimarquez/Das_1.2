namespace DAS.Domain.Common;

//Las fechas de auditoría las asigna la persistencia al guardar, no el dominio ni los servicios
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
