namespace DAS.Application.Security;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Encargado = "Encargado";
    public const string Consulta = "Consulta";

    //Roles que pueden modificar el inventario: [Authorize(Roles = AppRoles.Operadores)]
    public const string Operadores = Admin + "," + Encargado;

    public static readonly IReadOnlyList<string> All = [Admin, Encargado, Consulta];
}
