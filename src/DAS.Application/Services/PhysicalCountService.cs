using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.Common;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace DAS.Application.Services;

public sealed class PhysicalCountService(
    IProductRepository products,
    IMovementRepository movements,
    IPhysicalCountRepository physicalCounts,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<PhysicalCountService> logger) : IPhysicalCountService
{
    public async Task<ServiceResult<PhysicalCountDto>> RegisterAsync(IReadOnlyCollection<CountLineDto> lines, CancellationToken cancellationToken = default)
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

        var result = await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            var productIds = lines.Select(line => line.ProductId).ToList();
            var counted = (await products.GetByIdsAsync(productIds, token))
                .Where(product => product.IsActive)
                .ToDictionary(product => product.Id);
            if (counted.Count != productIds.Count)
            {
                return ServiceResult<PhysicalCountDto>.Fail("Uno de los productos contados no existe o está inactivo.");
            }

            var now = clock.GetLocalNow().DateTime;
            var userName = currentUser.UserName;
            var count = new PhysicalCount { CountedAt = now, UserName = userName };
            foreach (var line in lines)
            {
                count.AddLine(counted[line.ProductId], line.CountedStock);
            }

            //Se guarda primero el conteo porque su número aparece en el motivo de cada ajuste
            physicalCounts.Add(count);
            await unitOfWork.SaveChangesAsync(token);

            foreach (var line in count.Lines.Where(line => line.Difference != 0))
            {
                var reason = $"Conteo físico #{count.Id} · {(line.Difference > 0 ? "sobrante" : "faltante")}";
                var adjustment = counted[line.ProductId].AdjustToCount(line.CountedStock, reason, userName, now);
                if (adjustment is not null)
                {
                    movements.Add(adjustment);
                }
            }

            await unitOfWork.SaveChangesAsync(token);
            return ServiceResult<PhysicalCountDto>.Ok(count.ToDto());
        }, cancellationToken);

        if (result.Succeeded)
        {
            logger.LogInformation("Conteo físico {CountId} registrado con {LineCount} producto(s)", result.Value!.Id, result.Value.Lines.Count);
        }

        return result;
    }

    public async Task<PhysicalCountDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await physicalCounts.GetWithLinesAsync(id, cancellationToken))?.ToDto();

    public async Task<IReadOnlyList<PhysicalCountDto>> GetRecentAsync(int take, CancellationToken cancellationToken = default) =>
        (await physicalCounts.ListRecentAsync(take, cancellationToken)).Select(count => count.ToDto()).ToList();
}
