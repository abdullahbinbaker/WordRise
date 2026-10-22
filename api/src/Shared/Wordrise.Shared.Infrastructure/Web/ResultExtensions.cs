using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Shared.Kernel;

namespace Wordrise.Shared.Infrastructure.Web;

/// <summary>Turns a use-case <see cref="Result"/> into the HTTP response a controller returns.</summary>
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result) =>
        result.IsSuccess ? controller.Ok(result.Value) : controller.ToProblem(result.Error!);

    public static IActionResult ToActionResult(this ControllerBase controller, Result result) =>
        result.IsSuccess ? controller.NoContent() : controller.ToProblem(result.Error!);

    private static IActionResult ToProblem(this ControllerBase controller, Error error)
    {
        if (error.Type == ErrorType.Validation)
        {
            return controller.ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [error.Code] = [error.Message] }));
        }

        var status = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => throw new ArgumentOutOfRangeException(nameof(error), error.Type, null),
        };
        var problem = new ProblemDetails { Status = status, Title = error.Message, Extensions = { ["code"] = error.Code } };
        return new ObjectResult(problem) { StatusCode = status, ContentTypes = { "application/problem+json" } };
    }
}
