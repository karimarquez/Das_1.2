using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace DAS.Web.Extensions;

public static class ApplicationBuilderExtensions
{
    //Cultura fija es-SV: el model binding interpreta los decimales con punto, igual que los input number
    public static IApplicationBuilder UseSpanishCulture(this IApplicationBuilder app)
    {
        var culture = new CultureInfo("es-SV");
        return app.UseRequestLocalization(new RequestLocalizationOptions
        {
            DefaultRequestCulture = new RequestCulture(culture),
            SupportedCultures = [culture],
            SupportedUICultures = [culture]
        });
    }
}
