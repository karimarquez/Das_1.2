using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DAS.Services;

public sealed class MovementService(InMemoryInventoryStore store, IOptions<InventoryOptions> options, ILogger<MovementService> logger) : IMovementService
{
    public IReadOnlyList<MovementDto> Search(MovementQueryDto query)
    {
        lock (store.SyncRoot)
        {
            IEnumerable<InventoryMovement> movements = store.Movements;

            if (query.ProductId is int productId)
            {
                movements = movements.Where(movement => movement.ProductId == productId);
            }

            if (query.Type is MovementType type)
            {
                movements = movements.Where(movement => movement.Type == type);
            }

            if (query.From is DateTime from)
            {
                movements = movements.Where(movement => movement.RecordedAt.Date >= from.Date);
            }

            if (query.To is DateTime to)
            {
                movements = movements.Where(movement => movement.RecordedAt.Date <= to.Date);
            }

            movements = movements.OrderByDescending(movement => movement.RecordedAt).ThenByDescending(movement => movement.Id);

            if (query.Take is int take)
            {
                movements = movements.Take(take);
            }

            return movements.Select(movement => movement.ToDto()).ToList();
        }
    }

    public ServiceResult<MovementDto> Register(RegisterMovementDto input)
    {
        if (input.Type is not (MovementType.Entrada or MovementType.Consumo or MovementType.Merma))
        {
            return ServiceResult<MovementDto>.Fail("Los ajustes solo se generan desde el conteo físico.", nameof(RegisterMovementDto.Type));
        }

        lock (store.SyncRoot)
        {
            var product = store.Products.FirstOrDefault(item => item.Id == input.ProductId && item.IsActive);
            if (product is null)
            {
                return ServiceResult<MovementDto>.Fail("Selecciona un producto activo.", nameof(RegisterMovementDto.ProductId));
            }

            //Consumo y merma no pueden dejar el saldo en negativo
            if (input.Type != MovementType.Entrada && input.Quantity > product.Stock)
            {
                return ServiceResult<MovementDto>.Fail(
                    $"La existencia disponible de {product.Name} es {product.Stock:0.##} {product.Unit}.",
                    nameof(RegisterMovementDto.Quantity));
            }

            var delta = input.Type == MovementType.Entrada ? input.Quantity : -input.Quantity;
            var recordedAt = input.Date is DateTime date && date.Date < DateTime.Today
                ? date.Date.Add(DateTime.Now.TimeOfDay)
                : DateTime.Now;

            var movement = store.ApplyMovement(product, input.Type, delta, input.Reason, options.Value.DefaultUserName, recordedAt);
            logger.LogInformation("{Type} de {Quantity} {Unit} registrado para {ProductName}", movement.Type, movement.Quantity, movement.Unit, movement.ProductName);
            return ServiceResult<MovementDto>.Ok(movement.ToDto());
        }
    }
}
