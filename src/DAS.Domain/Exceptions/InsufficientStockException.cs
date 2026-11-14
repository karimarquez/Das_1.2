namespace DAS.Domain.Exceptions;

public sealed class InsufficientStockException(string productName, decimal available, string unit)
    : DomainException($"La existencia disponible de {productName} es {available:0.##} {unit}.")
{
    public string ProductName { get; } = productName;
    public decimal Available { get; } = available;
}
