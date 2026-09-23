using System.Net;
using System.Net.Http.Json;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Concrete;
using Moq;

namespace Gamestore.Tests.Services;

public class ExternalAuthenticationServiceTests
{
    [Fact]
    public async Task AuthenticateAsyncShouldReturnTrueForMatchingExternalUser()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                Email = "user@example.com",
                FirstName = "Test",
                LastName = "User",
            }),
        });

        var authenticated = await service.AuthenticateAsync("user@example.com", "Password");

        Assert.True(authenticated);
    }

    [Fact]
    public async Task AuthenticateAsyncShouldReturnFalseForUnauthorizedResponse()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.Unauthorized));

        var authenticated = await service.AuthenticateAsync("user@example.com", "WrongPassword");

        Assert.False(authenticated);
    }

    [Fact]
    public async Task AuthenticateAsyncShouldReturnFalseWhenReturnedEmailDoesNotMatch()
    {
        var service = CreateService(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new
            {
                Email = "another@example.com",
                FirstName = "Another",
                LastName = "User",
            }),
        });

        var authenticated = await service.AuthenticateAsync("user@example.com", "Password");

        Assert.False(authenticated);
    }

    [Fact]
    public async Task AuthenticateAsyncShouldReportUnavailableServiceForConnectionFailure()
    {
        var service = CreateService(_ => throw new HttpRequestException("Connection failed."));

        var exception = await Assert.ThrowsAsync<ExternalAuthenticationUnavailableException>(() =>
            service.AuthenticateAsync("user@example.com", "Password"));

        Assert.Equal("The external authentication service is unavailable.", exception.Message);
    }

    private static ExternalAuthenticationService CreateService(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
    {
        var httpClient = new HttpClient(new StubHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("http://localhost/"),
        };
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(ExternalAuthenticationService.HttpClientName))
            .Returns(httpClient);

        return new ExternalAuthenticationService(httpClientFactory.Object);
    }

    private sealed class StubHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(responseFactory(request));
        }
    }
}
