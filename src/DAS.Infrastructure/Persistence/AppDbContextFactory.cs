using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DAS.Infrastructure.Persistence;

//La usan las herramientas de dotnet ef para crear migraciones sin arrancar la aplicación web
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=PipirisNais.db")
            .Options;
        return new AppDbContext(options);
    }
}
