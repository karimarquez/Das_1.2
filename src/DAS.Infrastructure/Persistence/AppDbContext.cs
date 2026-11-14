using DAS.Domain.Entities;
using DAS.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace DAS.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<InventoryProduct> Products => Set<InventoryProduct>();
    public DbSet<InventoryMovement> Movements => Set<InventoryMovement>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<PhysicalCount> PhysicalCounts => Set<PhysicalCount>();
    public DbSet<PhysicalCountLine> PhysicalCountLines => Set<PhysicalCountLine>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        //Cada entidad se configura en su propia clase de Persistence/Configurations
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
