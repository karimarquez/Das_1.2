using DAS.Application.DTOs;
using DAS.Application.Security;
using DAS.Application.Services.Interfaces;
using DAS.Web.Extensions;
using DAS.Web.Models.Api;
using DAS.Web.OpenApi;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DAS.Web.Controllers.Api;

[ApiController]
[Authorize]
[AutoValidateAntiforgeryToken]
[Route("api/recetas")]
[Tags(ApiTags.Recipes)]
public sealed class RecipesApiController(IRecipeService recipeService) : ControllerBase
{
    [HttpGet]
    [EndpointSummary("Recetas con sus ingredientes")]
    [EndpointDescription("Lista las recetas y la cantidad por porción de cada ingrediente, expresada en la unidad base del producto.")]
    [ProducesResponseType<IReadOnlyList<RecipeDto>>(StatusCodes.Status200OK, "application/json")]
    public async Task<ActionResult<IReadOnlyList<RecipeDto>>> GetAll(CancellationToken cancellationToken) =>
        Ok(await recipeService.GetAllAsync(cancellationToken));

    [HttpPost("{id:int}/preparaciones")]
    [Authorize(Roles = AppRoles.Operadores)]
    [Consumes("application/json")]
    [EndpointSummary("Registra porciones preparadas de una receta")]
    [EndpointDescription("Convierte cada ingrediente a la unidad de inventario y descuenta todos en una sola transacción. Si un ingrediente no alcanza, no se descuenta ninguno y la respuesta nombra el faltante.")]
    [ProducesResponseType<RecipePreparationDto>(StatusCodes.Status201Created, "application/json")]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<ActionResult<RecipePreparationDto>> Prepare(int id, [FromBody] PrepareRecipeRequest input, CancellationToken cancellationToken)
    {
        if (await recipeService.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        var result = await recipeService.PrepareAsync(new PrepareRecipeDto(id, input.Servings), cancellationToken);
        if (!result.Succeeded)
        {
            ModelState.AddServiceError(result);
            return ValidationProblem(ModelState);
        }

        return StatusCode(StatusCodes.Status201Created, result.Value);
    }
}
