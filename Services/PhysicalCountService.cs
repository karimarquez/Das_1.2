using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DAS.Services;

public sealed class PhysicalCountService(AppDbContext db, IOptions<InventoryOptions> options, ILogger<PhysicalCountService> logger) : IPhysicalCountService
{
    public ServiceResult<PhysicalCountDto> Register(IReadOnlyCollection<CountLineDto> lines)
    {
        if (lines.Count == 0)
        {
            return ServiceResult<PhysicalCountDto>.Fail("Ingresa al menos una cantidad contada.");
        }

        if (lines.Any(line => line.CountedStock < 0))
        {
            return ServiceResult<PhysicalCountDto>.Fail("Las cantidades contadas no pueden ser negativas.");
        }

        if (lines.Select(line => line.ProductId).Distinct().Count() != lines.Count)
        {
            return ServiceResult<PhysicalCountDto>.Fail("Cada producto solo puede aparecer una vez en el conteo.");
        }

        var productIds = lines.Select(line => line.ProductId).ToList();
        var products = db.Products.Where(product => productIds.Contains(product.Id) && product.IsActive).ToList();
        if (products.Count != productIds.Count)
        {
            return ServiceResult<PhysicalCountDto>.Fail("Uno de los productos contados no existe o está inactivo.");
        }

        var now = DateTime.Now;
        var userName = options.Value.DefaultUserName;
        var count = new DAS.Models.Entities.PhysicalCount { CountedAt = now, UserName = userName };
        foreach (var line in lines)
        {
            var product = products.Single(item => item.Id == line.ProductId);
            count.Lines.Add(new DAS.Models.Entities.PhysicalCountLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Unit = product.Unit,
                TheoreticalStock = product.Stock,
                CountedStock = line.CountedStock,
                UnitCost = product.UnitCost
            });
        }

        using var transaction = db.Database.BeginTransaction();
        db.PhysicalCounts.Add(count);
        db.SaveChanges();
        foreach (var line in count.Lines)
        {
            var product = products.Single(item => item.Id == line.ProductId);
            var difference = line.CountedStock - product.Stock;
            if (difference != 0)
            {
                var reason = $"Conteo físico #{count.Id} · {(difference > 0 ? "sobrante" : "faltante")}";
                db.AddMovement(product, MovementType.Ajuste, difference, reason, userName, now);
            }
        }

        db.SaveChanges();
        transaction.Commit();
        logger.LogInformation("Conteo físico {CountId} registrado con {LineCount} producto(s)", count.Id, count.Lines.Count);
        return ServiceResult<PhysicalCountDto>.Ok(count.ToDto());
    }

    public PhysicalCountDto? GetById(int id)
    {
        var count = db.PhysicalCounts.AsNoTracking().Include(item => item.Lines).FirstOrDefault(item => item.Id == id);
        return count?.ToDto();
    }

    public IReadOnlyList<PhysicalCountDto> GetRecent(int take)
    {
        var counts = db.PhysicalCounts.AsNoTracking().Include(count => count.Lines)
                .OrderByDescending(count => count.CountedAt)
                .ThenByDescending(count => count.Id)
                .Take(Math.Clamp(take, 1, 100))
                .ToList();
        return counts.Select(count => count.ToDto()).ToList();
    }
}
