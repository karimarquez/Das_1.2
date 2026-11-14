using DAS.Application;
using DAS.Application.Abstractions;
using DAS.Application.Abstractions.Persistence;
using DAS.Application.Services.Interfaces;
using DAS.Infrastructure;
using DAS.Tests.Fakes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DAS.Tests.Architecture;

public class DependencyInjectionTests
{
    [Fact]
    public void ElContenedorResuelveCadaInterfazConSuImplementacion()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:" })
            .Build();
        var services = new ServiceCollection()
            .AddLogging()
            .AddApplication()
            .AddInfrastructure(configuration)
            .AddScoped<ICurrentUser>(_ => new FakeCurrentUser());

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        using var scope = provider.CreateScope();
        var resolve = scope.ServiceProvider;

        Assert.NotNull(resolve.GetRequiredService<IMovementService>());
        Assert.NotNull(resolve.GetRequiredService<IPhysicalCountService>());
        Assert.NotNull(resolve.GetRequiredService<IRecipeService>());
        Assert.NotNull(resolve.GetRequiredService<IReportService>());
        Assert.NotNull(resolve.GetRequiredService<IUnitOfWork>());
        Assert.NotEmpty(resolve.GetServices<IReportExporter>());

        //Consultas y comandos de productos los atiende la misma instancia dentro de la petición
        Assert.Same(resolve.GetRequiredService<IProductService>(), resolve.GetRequiredService<IProductQueryService>());
    }
}
