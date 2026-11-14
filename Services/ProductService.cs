using DAS.Configuration;
using DAS.Data;
using DAS.Mappings;
using DAS.Models.DTOs;
using DAS.Models.Entities;
using DAS.Models.Enums;
using DAS.Services.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;

namespace DAS.Services;

public sealed class ProductService(AppDbContext db, IOptions<InventoryOptions> options, ILogger<ProductService> logger) : IProductService
{
    public IReadOnlyList<ProductDto> GetActive()
        => db.Products.AsNoTracking().Where(product => product.IsActive)
            .OrderBy(product => product.Name).ToList().Select(product => product.ToDto()).ToList();

    public IReadOnlyList<ProductStockDto> SearchStock(string? search, int page, int pageSize)
    {
        var products = db.Products.AsNoTracking().Where(product => product.IsActive);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            products = products.Where(product => product.Name.ToLower().Contains(term)
                || product.Category.ToLower().Contains(term));
        }

        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);
        return products.OrderBy(product => product.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductStockDto(
                product.Id, product.Name, product.Category, product.Stock, product.Unit,
                product.MinimumStock, product.Stock <= product.MinimumStock))
            .ToList();
    }

    public IReadOnlyList<ProductDto> GetAll()
        => db.Products.AsNoTracking().OrderBy(product => product.Name)
            .ToList().Select(product => product.ToDto()).ToList();

    public ProductDto? GetById(int id)
        => db.Products.AsNoTracking().FirstOrDefault(product => product.Id == id)?.ToDto();

    public bool NameExists(string name, int? excludeId = null) =>
        db.Products.Any(product => product.Id != excludeId && product.Name.ToLower() == name.Trim().ToLower());

    public ServiceResult<ProductDto> Create(SaveProductDto input)
    {
        if (NameExists(input.Name))
        {
            return ServiceResult<ProductDto>.Fail("Ya existe un producto con ese nombre.", nameof(SaveProductDto.Name));
        }

        var now = DateTime.Now;
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
            Location = input.Location,
            CreatedAt = now,
            UpdatedAt = now
        };
        using var transaction = db.Database.BeginTransaction();
        db.Products.Add(product);
        db.SaveChanges();

        if (input.InitialStock > 0)
        {
            db.AddMovement(product, MovementType.Entrada, input.InitialStock, "Existencia inicial", options.Value.DefaultUserName, now);
            db.SaveChanges();
        }
        transaction.Commit();

        logger.LogInformation("Producto {ProductId} creado: {ProductName}", product.Id, product.Name);
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }

    public ServiceResult<ProductDto> Update(int id, SaveProductDto input)
    {
        var product = db.Products.FirstOrDefault(item => item.Id == id);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("El producto no existe.");
        }

        if (NameExists(input.Name, id))
        {
            return ServiceResult<ProductDto>.Fail("Ya existe un producto con ese nombre.", nameof(SaveProductDto.Name));
        }

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
        db.SaveChanges();
        logger.LogInformation("Producto {ProductId} actualizado", product.Id);
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }

    public ServiceResult<ProductDto> SetActive(int id, bool isActive)
    {
        var product = db.Products.FirstOrDefault(item => item.Id == id);
        if (product is null)
        {
            return ServiceResult<ProductDto>.Fail("El producto no existe.");
        }

        product.IsActive = isActive;
        product.UpdatedAt = DateTime.Now;
        db.SaveChanges();
        logger.LogInformation("Producto {ProductId} {State}", product.Id, isActive ? "reactivado" : "desactivado");
        return ServiceResult<ProductDto>.Ok(product.ToDto());
    }
}
