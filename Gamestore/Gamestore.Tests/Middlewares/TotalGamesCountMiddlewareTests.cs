using Gamestore.API.Middlewares;
using Gamestore.BLL.Services.Abstract;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Moq;

namespace Gamestore.Tests.Middlewares;

public class TotalGamesCountMiddlewareTests : IDisposable
{
    private readonly Mock<IGameService> _mockGameService;
    private readonly MemoryCache _cache;

    public TotalGamesCountMiddlewareTests()
    {
        _mockGameService = new Mock<IGameService>();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    public void Dispose()
    {
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task InvokeAsyncShouldAddTotalGamesHeader()
    {
        _mockGameService.Setup(service => service.GetTotalCountAsync())
            .ReturnsAsync(42);

        var context = await RunMiddlewareAsync();

        Assert.True(context.Response.Headers.ContainsKey("x-total-numbers-of-games"));
        Assert.Equal("42", context.Response.Headers["x-total-numbers-of-games"].ToString());
    }

    [Fact]
    public async Task InvokeAsyncShouldCallServiceOnFirstRequest()
    {
        _mockGameService.Setup(service => service.GetTotalCountAsync())
            .ReturnsAsync(10);

        await RunMiddlewareAsync();

        _mockGameService.Verify(service => service.GetTotalCountAsync(), Times.Once);
    }

    [Fact]
    public async Task InvokeAsyncShouldUseCacheOnSecondRequest()
    {
        _mockGameService.Setup(service => service.GetTotalCountAsync())
            .ReturnsAsync(10);

        await RunMiddlewareAsync();
        await RunMiddlewareAsync();

        _mockGameService.Verify(service => service.GetTotalCountAsync(), Times.Once);
    }

    private async Task<HttpContext> RunMiddlewareAsync()
    {
        var responseFeature = new CapturingResponseFeature();
        var context = new DefaultHttpContext();
        context.Features.Set<IHttpResponseFeature>(responseFeature);

        var middleware = new TotalGamesCountMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context, _mockGameService.Object, _cache);
        await responseFeature.FireOnStartingAsync();

        return context;
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
