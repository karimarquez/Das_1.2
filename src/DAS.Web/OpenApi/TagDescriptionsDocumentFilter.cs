using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DAS.Web.OpenApi;

public sealed class TagDescriptionsDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        foreach (var tag in swaggerDoc.Tags ?? new HashSet<OpenApiTag>())
        {
            var match = ApiTags.Descriptions.FirstOrDefault(item => item.Name == tag.Name);
            if (match.Description is not null)
            {
                tag.Description = match.Description;
            }
        }
    }
}
