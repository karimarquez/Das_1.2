namespace DAS.Web.Security;

public static class SecurityPolicies
{
    public const string CredentialsRateLimit = "credenciales";
    public const string AntiforgeryHeaderName = "X-CSRF-TOKEN";
    public const string CredentialAttemptsSetting = "Seguridad:IntentosDeCredencialesPorMinuto";
    public const int DefaultCredentialAttemptsPerMinute = 10;

    public static bool IsApiRequest(HttpRequest request) => request.Path.StartsWithSegments("/api");
}
