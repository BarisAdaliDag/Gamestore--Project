using System.Net;
using System.Net.Http.Json;
using Gamestore.BLL.Exceptions;
using Gamestore.BLL.Services.Abstract;

namespace Gamestore.BLL.Services.Concrete;

public sealed class ExternalAuthenticationService(IHttpClientFactory httpClientFactory)
    : IExternalAuthenticationService
{
    public const string HttpClientName = "AuthService";

    public async Task<bool> AuthenticateAsync(string email, string password)
    {
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            using var response = await client.PostAsJsonAsync(
                "api/auth",
                new ExternalAuthenticationRequest(email, password));

            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<ExternalUserResponse>();
            return user is not null
                && string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            throw new ExternalAuthenticationUnavailableException(exception);
        }
    }

    private sealed record ExternalAuthenticationRequest(string Email, string Password);

    private sealed record ExternalUserResponse(string Email, string FirstName, string LastName);
}
