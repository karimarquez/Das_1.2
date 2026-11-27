using DAS.Application.Services;
using DAS.Infrastructure.Persistence;
using DAS.Infrastructure.Persistence.Repositories;
using DAS.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace DAS.Tests.Integration;

//Mismo armado que hace el contenedor de dependencias, pero con un contexto de prueba
public sealed class EfServiceSet(AppDbContext context, TimeProvider clock)
{
    private readonly UnitOfWork _unitOfWork = new(context);
    private readonly ProductRepository _products = new(context);
    private readonly MovementRepository _movements = new(context);
    private readonly RecipeRepository _recipes = new(context);
    private readonly PhysicalCountRepository _counts = new(context);
    private readonly FakeCurrentUser _user = new();

    public ProductService Products => new(_products, _movements, _unitOfWork, _user, clock, NullLogger<ProductService>.Instance);

    public MovementService Movements => new(_products, _movements, _unitOfWork, _user, clock, NullLogger<MovementService>.Instance);

    public PhysicalCountService Counts => new(_products, _movements, _counts, _unitOfWork, _user, clock, NullLogger<PhysicalCountService>.Instance);

    public RecipeService Recipes => new(_recipes, _products, _movements, _unitOfWork, _user, clock, NullLogger<RecipeService>.Instance);

    public ReportService Reports => new(_products, _movements, NullLogger<ReportService>.Instance);
}
