using Gamestore.API.Middlewares;
using Gamestore.BLL.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;

namespace Gamestore.Tests.Middlewares;

public class TotalGamesCountMiddlewareTests
{
    [Fact]
    public async Task InvokeAsyncShouldAddTotalGamesHeader()
    {
        var mockGameService = new Mock<IGameService>();
        mockGameService.Setup(service => service.GetTotalCountAsync())
            .ReturnsAsync(42);

        var responseFeature = new CapturingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);

        var nextCalled = false;
        async Task Next(HttpContext httpContext)
        {
            nextCalled = true;
            await httpContext.Response.WriteAsync(string.Empty);
        }

        var middleware = new TotalGamesCountMiddleware(Next);

        await middleware.InvokeAsync(context, mockGameService.Object);
        await responseFeature.FireOnStartingAsync();

        Assert.True(nextCalled);
        Assert.True(context.Response.Headers.ContainsKey("x-total-numbers-of-games"));
        Assert.Equal("42", context.Response.Headers["x-total-numbers-of-games"].ToString());
        mockGameService.Verify(service => service.GetTotalCountAsync(), Times.Once);
    }

    private sealed class CapturingResponseFeature : IHttpResponseFeature
    {
        private readonly List<(Func<object, Task> Callback, object State)> _onStartingCallbacks = [];

        public int StatusCode { get; set; } = StatusCodes.Status200OK;

        public string? ReasonPhrase { get; set; }

        public IHeaderDictionary Headers { get; set; } = new HeaderDictionary();

        public Stream Body { get; set; } = new MemoryStream();

        public bool HasStarted { get; private set; }

        public void OnStarting(Func<object, Task> callback, object state)
        {
            _onStartingCallbacks.Add((callback, state));
        }

        public void OnCompleted(Func<object, Task> callback, object state)
        {
        }

        public async Task FireOnStartingAsync()
        {
            HasStarted = true;
            foreach (var (callback, state) in _onStartingCallbacks)
            {
                await callback(state);
            }
        }
    }
}
