using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Ordering.API;

public static class AuthExtensions
{
    public static IServiceCollection AddDevJwtAuth(this IServiceCollection services, IConfiguration cfg)
    {
        var key = cfg["JWT_KEY"] ?? "dev_super_secret_key_change_me_please_32chars";
        var issuer = cfg["JWT_ISSUER"] ?? "ShopSphere.Identity";
        var audience = cfg["JWT_AUDIENCE"] ?? "ShopSphere";

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.RequireHttpsMetadata = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = issuer,
                    ValidAudience = audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key))
                };
            });

        services.AddAuthorization();
        return services;
    }
}
