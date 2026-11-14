using DAS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DAS.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.HasMany(recipe => recipe.Ingredients).WithOne()
            .HasForeignKey("RecipeId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        //Clave sombra: el ingrediente no necesita Id en el dominio
        builder.Property<int>("Id");
        builder.HasKey("Id");
        builder.HasOne<InventoryProduct>().WithMany()
            .HasForeignKey(ingredient => ingredient.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(ingredient => ingredient.QuantityPerServing).HasPrecision(18, 4);
    }
}
