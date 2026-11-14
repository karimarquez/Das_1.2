using DAS.Application.Abstractions;
using DAS.Web.Configuration;
using Microsoft.Extensions.Options;

namespace DAS.Web.Security;

//Los movimientos y conteos quedan a nombre de la cuenta con sesión iniciada
public sealed class HttpCurrentUser(IHttpContextAccessor accessor, IOptions<InventoryOptions> options) : ICurrentUser
{
    public string UserName =>
        accessor.HttpContext?.User.Identity is { IsAuthenticated: true, Name: { Length: > 0 } name }
            ? name
            : options.Value.DefaultUserName;
}
