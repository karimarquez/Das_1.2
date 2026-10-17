using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DAS.Services;

public sealed class PhysicalCountService(InMemoryInventoryStore store, IOptions<InventoryOptions> options, ILogger<PhysicalCountService> logger) : IPhysicalCountService
{
    public ServiceResult<PhysicalCountDto> Register(IReadOnlyCollection<CountLineDto> lines)
    {
        if (lines.Count == 0)
        {
            return ServiceResult<PhysicalCountDto>.Fail("Ingresa al menos una cantidad contada.");
        }

        lock (store.SyncRoot)
        {
            //Se validan todas las líneas antes de ajustar cualquier existencia
            var counted = new List<(InventoryProduct Product, decimal Quantity)>();
            foreach (var line in lines)
            {
                var product = store.Products.FirstOrDefault(item => item.Id == line.ProductId && item.IsActive);
                if (product is null)
                {
                    return ServiceResult<PhysicalCountDto>.Fail("Uno de los productos contados no existe o está inactivo.");
                }

                if (line.CountedStock < 0)
                {
                    return ServiceResult<PhysicalCountDto>.Fail($"La cantidad contada de {product.Name} no puede ser negativa.");
                }

                if (counted.Any(item => item.Product.Id == product.Id))
                {
                    return ServiceResult<PhysicalCountDto>.Fail($"{product.Name} aparece más de una vez en el conteo.");
                }

                counted.Add((product, line.CountedStock));
            }

            var now = DateTime.Now;
            var userName = options.Value.DefaultUserName;
            var count = store.AddPhysicalCount(new PhysicalCount { CountedAt = now, UserName = userName });

            foreach (var (product, quantity) in counted)
            {
                count.Lines.Add(new PhysicalCountLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    Unit = product.Unit,
                    TheoreticalStock = product.Stock,
                    CountedStock = quantity,
                    UnitCost = product.UnitCost
                });

                var difference = quantity - product.Stock;
                if (difference != 0)
                {
                    var reason = $"Conteo físico #{count.Id} · {(difference > 0 ? "sobrante" : "faltante")}";
                    store.ApplyMovement(product, MovementType.Ajuste, difference, reason, userName, now);
                }
            }

            logger.LogInformation("Conteo físico {CountId} registrado con {LineCount} producto(s)", count.Id, count.Lines.Count);
            return ServiceResult<PhysicalCountDto>.Ok(count.ToDto());
        }
    }

    public PhysicalCountDto? GetById(int id)
    {
        lock (store.SyncRoot)
        {
            return store.PhysicalCounts.FirstOrDefault(count => count.Id == id)?.ToDto();
        }
    }

    public IReadOnlyList<PhysicalCountDto> GetRecent(int take)
    {
        lock (store.SyncRoot)
        {
            return store.PhysicalCounts
                .OrderByDescending(count => count.CountedAt)
                .ThenByDescending(count => count.Id)
                .Take(take)
                .Select(count => count.ToDto())
                .ToList();
        }
    }
}
