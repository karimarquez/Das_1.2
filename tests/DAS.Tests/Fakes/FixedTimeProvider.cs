namespace DAS.Tests.Fakes;

//Reloj fijo: las pruebas no dependen de la fecha en que se ejecutan
public sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
