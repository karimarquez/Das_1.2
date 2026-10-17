namespace DAS.Models.DTOs;

//Resultado de una operación de negocio; Field indica el campo del formulario relacionado con el error
public class ServiceResult
{
    protected ServiceResult(bool succeeded, string? error, string? field)
    {
        Succeeded = succeeded;
        Error = error;
        Field = field;
    }

    public bool Succeeded { get; }
    public string? Error { get; }
    public string? Field { get; }

    public static ServiceResult Ok() => new(true, null, null);

    public static ServiceResult Fail(string error, string? field = null) => new(false, error, field);
}

public sealed class ServiceResult<T> : ServiceResult
{
    private ServiceResult(bool succeeded, T? value, string? error, string? field) : base(succeeded, error, field)
    {
        Value = value;
    }

    public T? Value { get; }

    public static ServiceResult<T> Ok(T value) => new(true, value, null, null);

    public static new ServiceResult<T> Fail(string error, string? field = null) => new(false, default, error, field);
}
