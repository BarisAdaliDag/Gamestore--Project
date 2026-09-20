using Gamestore.API.Middlewares;
using Gamestore.BLL.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace Gamestore.Tests.Middlewares;

public class GlobalExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncShouldReturnConflictWhenEntityIsInUse()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new GlobalExceptionMiddleware(
            _ => throw new EntityInUseException("Publisher", Guid.NewGuid()),
            Mock.Of<ILogger<GlobalExceptionMiddleware>>());

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
    }
}
