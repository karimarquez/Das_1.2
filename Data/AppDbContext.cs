using DAS.Models.Entities;
using DAS.Models.Auth;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAS.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<InventoryProduct> Products => Set<InventoryProduct>();
    public DbSet<InventoryMovement> Movements => Set<InventoryMovement>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<PhysicalCount> PhysicalCounts => Set<PhysicalCount>();
    public DbSet<PhysicalCountLine> PhysicalCountLines => Set<PhysicalCountLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<InventoryProduct>(e =>
        {
            e.HasIndex(p => p.Name).IsUnique();
            e.Property(p => p.Name).HasMaxLength(80).IsRequired();
            e.Property(p => p.Stock).HasPrecision(18, 4);
            e.Property(p => p.MinimumStock).HasPrecision(18, 4);
            e.Property(p => p.UnitCost).HasPrecision(18, 2);
            e.Property(p => p.BaseUnitsPerStockUnit).HasPrecision(18, 4);
            e.Ignore(p => p.IsLowStock);
        });

        b.Entity<InventoryMovement>(e =>
        {
            e.HasOne<InventoryProduct>().WithMany()
             .HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Property(m => m.Quantity).HasPrecision(18, 4);
            e.Property(m => m.StockBefore).HasPrecision(18, 4);
            e.Property(m => m.StockAfter).HasPrecision(18, 4);
        });

        b.Entity<Recipe>(e =>
        {
            e.HasMany(r => r.Ingredients).WithOne()
             .HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RecipeIngredient>(e =>
        {
            e.Property<int>("Id");
            e.HasKey("Id");
            e.HasOne<InventoryProduct>().WithMany()
             .HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Property(i => i.QuantityPerServing).HasPrecision(18, 4);
        });

        b.Entity<PhysicalCount>(e =>
        {
            e.HasMany(c => c.Lines).WithOne()
             .HasForeignKey("PhysicalCountId").OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<PhysicalCountLine>(e =>
        {
            e.Property<int>("Id");
            e.HasKey("Id");
            e.HasOne<InventoryProduct>().WithMany()
             .HasForeignKey(l => l.ProductId).OnDelete(DeleteBehavior.Restrict);
            e.Property(l => l.TheoreticalStock).HasPrecision(18, 4);
            e.Property(l => l.CountedStock).HasPrecision(18, 4);
            e.Property(l => l.UnitCost).HasPrecision(18, 2);
            e.Ignore(l => l.Difference);
        });
    }
}