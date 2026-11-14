using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.Common;
using DAS.Application.DTOs;
using DAS.Application.Mappings;
using DAS.Application.Services.Interfaces;
using DAS.Domain.Entities;
using DAS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace DAS.Application.Services;

public sealed class ProductService(
    IProductRepository products,
    IMovementRepository movements,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider clock,
    ILogger<ProductService> logger) : IProductService
{
    private const string DuplicateNameMessage = "Ya existe un producto con ese nombre.";

    public async Task<IReadOnlyList<ProductDto>> GetActiveAsync(CancellationToken cancellationToken = default) =>
        (await products.ListOrderedAsync(activeOnly: true, cancellationToken)).Select(product => product.ToDto()).ToList();

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await products.ListOrderedAsync(activeOnly: false, cancellationToken)).Select(product => product.ToDto()).ToList();

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        (await products.GetByIdAsync(id, cancellationToken))?.ToDto();

    public async Task<IReadOnlyList<ProductStockDto>> SearchStockAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var found = await products.SearchActiveAsync(search, Math.Max(page, 1), Math.Clamp(pageSize, 1, 100), cancellationToken);
        return found.Select(product => product.ToStockDto()).ToList();
    }

    public Task<bool> NameExistsAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
        products.NameExistsAsync(name, excludeId, cancellationToken);

    public async Task<ServiceResult<ProductDto>> CreateAsync(SaveProductDto input, CancellationToken cancellationToken = default)
    {
        if (await products.NameExistsAsync(input.Name, null, cancellationToken))
        {
            return ServiceResult<ProductDto>.Fail(DuplicateNameMessage, nameof(SaveProductDto.Name));
        }

        var product = new InventoryProduct
        {
            Name = input.Name,
            Category = input.Category,
            Presentation = input.Presentation,
            Unit = input.Unit,
            BaseUnit = input.BaseUnit,
            BaseUnitsPerStockUnit = input.BaseUnitsPerStockUnit,
            MinimumStock = input.MinimumStock,
            UnitCost = input.UnitCost,
            Location = input.Location
        };

        //El producto necesita su Id antes de registrar la existencia inicial; ambos pasos van en una transacción
        await unitOfWork.ExecuteInTransactionAsync(async token =>
        {
            products.Add(product);
            await unitOfWork.SaveChangesAsync(token);

            if (input.InitialStock > 0)
            {
                var now = clock.GetLocalNow().DateTime;
                movements.Add(product.RegisterMovement(MovementType.Entrada, input.InitialStock, "Existencia inicial", currentUser.UserName, now));
                await unitOfWork.SaveChangesAsync(token);
            }

            return product;
        }, cancellationToken);

        logger.LogInformation("Producto {ProductId} creado: {ProductName}", product.Id, product.Name);
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(int id, SaveProductDto input, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("El producto no existe.");
        }

        if (await products.NameExistsAsync(input.Name, id, cancellationToken))
        {
            return ServiceResult<ProductDto>.Fail(DuplicateNameMessage, nameof(SaveProductDto.Name));
        }

        //La existencia no se edita aquí: solo cambia con movimientos o conteos
        product.Name = input.Name;
        product.Category = input.Category;
        product.Presentation = input.Presentation;
        product.Unit = input.Unit;
        product.BaseUnit = input.BaseUnit;
        product.BaseUnitsPerStockUnit = input.BaseUnitsPerStockUnit;
        product.MinimumStock = input.MinimumStock;
        product.UnitCost = input.UnitCost;
        product.Location = input.Location;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Producto {ProductId} actualizado", product.Id);
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }

    public async Task<ServiceResult<ProductDto>> SetActiveAsync(int id, bool isActive, CancellationToken cancellationToken = default)
    {
        var product = await products.GetByIdAsync(id, cancellationToken);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("El producto no existe.");
        }

        product.IsActive = isActive;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Producto {ProductId} {State}", product.Id, isActive ? "reactivado" : "desactivado");
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }
}
