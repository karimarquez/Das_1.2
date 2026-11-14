using DAS.Infrastructure.Persistence;
using DAS.Infrastructure.Persistence.Interceptors;
using DAS.Tests.Fakes;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace DAS.Tests.Integration;

//Base SQLite en memoria con las migraciones reales del proyecto; vive mientras la conexión esté abierta
public sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public SqliteTestDatabase()
    {
        _connection.Open();
        using var context = CreateContext();
        context.Database.Migrate();
    }

    public TimeProvider Clock { get; } = new FixedTimeProvider(TestData.Now);

    public AppDbContext CreateContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite(_connection)
        .AddInterceptors(new AuditableEntityInterceptor(Clock))
        .Options);

    public void Dispose() => _connection.Dispose();
}
