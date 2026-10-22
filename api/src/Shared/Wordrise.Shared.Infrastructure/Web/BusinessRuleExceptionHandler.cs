using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Wordrise.Shared.Kernel;

namespace Wordrise.Shared.Infrastructure.Web;

/// <summary>A broken domain rule is the caller's mistake: answer 400 with the rule's code.</summary>
public sealed class BusinessRuleExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not BusinessRuleException rule)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = rule.Message,
                Extensions = { ["code"] = rule.Code },
            },
        });
    }
}
