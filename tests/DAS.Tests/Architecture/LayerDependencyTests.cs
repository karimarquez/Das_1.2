using System.Reflection;
using DAS.Application.Abstractions.Persistence;
using DAS.Domain.Entities;
using DAS.Infrastructure.Persistence;
using DAS.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Tests.Architecture;

//La regla de dependencias de la arquitectura limpia se comprueba en cada ejecución de pruebas
public class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(InventoryProduct).Assembly;
    private static readonly Assembly Application = typeof(IUnitOfWork).Assembly;
    private static readonly Assembly Infrastructure = typeof(AppDbContext).Assembly;
    private static readonly Assembly Web = typeof(InventoryController).Assembly;

    [Fact]
    public void Dominio_NoDependeDeOtrasCapasNiDeFrameworks()
    {
        Assert.DoesNotContain(ReferencesOf(Domain), name =>
            name.StartsWith("DAS.") || name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Aplicacion_SoloDependeDelDominio()
    {
        var references = ReferencesOf(Application);

        Assert.Equal(new[] { "DAS.Domain" }, references.Where(name => name.StartsWith("DAS.")));
        Assert.DoesNotContain(references, name => name.StartsWith("Microsoft.EntityFrameworkCore") || name.StartsWith("Microsoft.AspNetCore"));
    }

    [Fact]
    public void Infraestructura_NoDependeDeLaCapaWeb()
    {
        Assert.DoesNotContain(ReferencesOf(Infrastructure), name => name == "DAS.Web");
    }

    [Fact]
    public void Controladores_NoUsanRepositoriosNiDbContextDirectamente()
    {
        var forbidden = new[] { "DAS.Infrastructure.Persistence", "DAS.Application.Abstractions.Persistence" };
        var controllers = Web.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract);

        foreach (var controller in controllers)
        {
            var dependencies = controller.GetConstructors()
                .SelectMany(constructor => constructor.GetParameters())
                .SelectMany(parameter => Expand(parameter.ParameterType));

            Assert.DoesNotContain(dependencies, type => forbidden.Any(prefix => type.Namespace?.StartsWith(prefix) == true));
        }
    }

    [Fact]
    public void Servicios_DependenSoloDeAbstracciones()
    {
        var services = Application.GetTypes()
            .Where(type => type.Namespace == "DAS.Application.Services" && type.IsClass && !type.IsAbstract && !type.IsNested && type.GetInterfaces().Length > 0);

        Assert.NotEmpty(services);
        foreach (var service in services)
        {
            var parameters = service.GetConstructors().SelectMany(constructor => constructor.GetParameters());
            Assert.All(parameters, parameter => Assert.True(
                parameter.ParameterType.IsInterface || parameter.ParameterType.IsAbstract,
                $"{service.Name} recibe la clase concreta {parameter.ParameterType.Name}"));
        }
    }

    private static IReadOnlyList<string> ReferencesOf(Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToList();

    private static IEnumerable<Type> Expand(Type type) =>
        type.IsGenericType ? [type, .. type.GetGenericArguments().SelectMany(Expand)] : [type];
}
