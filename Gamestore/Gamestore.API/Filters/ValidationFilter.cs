using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Gamestore.API.Filters;

public class ValidationFilter<T>(IValidator<T> validator) : IAsyncActionFilter
    where T : class
{
    private readonly IValidator<T> _validator = validator;

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var request = context.ActionArguments.Values.OfType<T>().FirstOrDefault();
        if (request == null)
        {
            await next();
            return;
        }

        var result = await _validator.ValidateAsync(
            request,
            context.HttpContext.RequestAborted);

        if (!result.IsValid)
        {
            context.Result = new BadRequestObjectResult(new
            {
                statusCode = StatusCodes.Status400BadRequest,
                message = string.Join(" ", result.Errors.Select(error => error.ErrorMessage)),
            });
            return;
        }

        await next();
    }
}
