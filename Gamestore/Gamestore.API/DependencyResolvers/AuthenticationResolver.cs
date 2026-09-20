using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Gamestore.BLL.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Gamestore.API.DependencyResolvers;

public static class AuthenticationResolver
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = GetRequiredSetting(configuration, "Jwt:Issuer");
        var audience = GetRequiredSetting(configuration, "Jwt:Audience");
        var signingKey = GetRequiredSetting(configuration, "Jwt:Key");

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var authorizationHeader = context.Request.Headers.Authorization.ToString();

                        if (!string.IsNullOrWhiteSpace(authorizationHeader)
                            && !authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                        {
                            context.Token = authorizationHeader.Trim();
                        }

                        return Task.CompletedTask;
                    },
                };
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                    ValidateLifetime = true,
                    RequireExpirationTime = true,
                    RequireSignedTokens = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = JwtRegisteredClaimNames.UniqueName,
                    RoleClaimType = JwtClaimNames.Role,
                };
            });

        services.AddAuthorization();
        return services;
    }

    private static string GetRequiredSetting(IConfiguration configuration, string key)
    {
        return configuration[key]
            ?? throw new InvalidOperationException($"{key} configuration is required.");
    }
}
