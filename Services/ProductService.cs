using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace DAS.Services;

public sealed class ProductService(InMemoryInventoryStore store, IOptions<InventoryOptions> options, ILogger<ProductService> logger) : IProductService
{
    public IReadOnlyList<ProductDto> GetActive()
    {
        lock (store.SyncRoot)
        {
            return store.Products
                .Where(product => product.IsActive)
                .OrderBy(product => product.Name)
                .Select(product => product.ToDto())
                .ToList();
        }
    }

    public IReadOnlyList<ProductDto> GetAll()
    {
        lock (store.SyncRoot)
        {
            return store.Products
                .OrderBy(product => product.Name)
                .Select(product => product.ToDto())
                .ToList();
        }
    }

    public ProductDto? GetById(int id)
    {
        lock (store.SyncRoot)
        {
            return store.Products.FirstOrDefault(product => product.Id == id)?.ToDto();
        }
    }

    public bool NameExists(string name, int? excludeId = null)
    {
        lock (store.SyncRoot)
        {
            return IsNameTaken(name, excludeId);
        }
    }

    public ServiceResult<ProductDto> Create(SaveProductDto input)
    {
        lock (store.SyncRoot)
        {
            if (IsNameTaken(input.Name, null))
            {
                return ServiceResult<ProductDto>.Fail("Ya existe un producto con ese nombre.", nameof(SaveProductDto.Name));
            }

            var now = DateTime.Now;
            var product = store.AddProduct(new InventoryProduct
            {
                Name = input.Name,
                Category = input.Category,
                Presentation = input.Presentation,
                Unit = input.Unit,
                BaseUnit = input.BaseUnit,
                BaseUnitsPerStockUnit = input.BaseUnitsPerStockUnit,
                MinimumStock = input.MinimumStock,
                UnitCost = input.UnitCost,
                Location = input.Location,
                CreatedAt = now,
                UpdatedAt = now
            });

            //La existencia inicial también se registra como movimiento para conservar la trazabilidad
            if (input.InitialStock > 0)
            {
                store.ApplyMovement(product, MovementType.Entrada, input.InitialStock, "Existencia inicial", options.Value.DefaultUserName, now);
            }

            logger.LogInformation("Producto {ProductId} creado: {ProductName}", product.Id, product.Name);
            return ServiceResult<ProductDto>.Ok(product.ToDto());
        }
    }

    public ServiceResult<ProductDto> Update(int id, SaveProductDto input)
    {
        lock (store.SyncRoot)
        {
            var product = store.Products.FirstOrDefault(item => item.Id == id);
            if (product is null)
            {
                return ServiceResult<ProductDto>.Fail("El producto no existe.");
            }

            if (IsNameTaken(input.Name, id))
            {
                return ServiceResult<ProductDto>.Fail("Ya existe un producto con ese nombre.", nameof(SaveProductDto.Name));
            }

            //La existencia no se edita aquí: solo cambia mediante movimientos o conteos
            product.Name = input.Name;
            product.Category = input.Category;
            product.Presentation = input.Presentation;
            product.Unit = input.Unit;
            product.BaseUnit = input.BaseUnit;
            product.BaseUnitsPerStockUnit = input.BaseUnitsPerStockUnit;
            product.MinimumStock = input.MinimumStock;
            product.UnitCost = input.UnitCost;
            product.Location = input.Location;
            product.UpdatedAt = DateTime.Now;

            logger.LogInformation("Producto {ProductId} actualizado", product.Id);
            return ServiceResult<ProductDto>.Ok(product.ToDto());
        }
    }

    public ServiceResult<ProductDto> SetActive(int id, bool isActive)
    {
        lock (store.SyncRoot)
        {
            var product = store.Products.FirstOrDefault(item => item.Id == id);
            if (product is null)
            {
                return ServiceResult<ProductDto>.Fail("El producto no existe.");
            }

            product.IsActive = isActive;
            product.UpdatedAt = DateTime.Now;
            logger.LogInformation("Producto {ProductId} {State}", product.Id, isActive ? "reactivado" : "desactivado");
            return ServiceResult<ProductDto>.Ok(product.ToDto());
        }
    }

    private bool IsNameTaken(string name, int? excludeId) =>
        store.Products.Any(product => product.Id != excludeId
            && string.Equals(product.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
}
