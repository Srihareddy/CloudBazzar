using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using ShopSphere.Observability;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, lc) =>
{
    var seq = ctx.Configuration["SEQ_URL"] ?? "http://seq:80";
    lc.Enrich.FromLogContext()
      .WriteTo.Console()
      .WriteTo.Seq(seq);
});

builder.Services.AddShopSphereOpenTelemetry("Identity.API");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.MapPost("/token", (TokenRequest req, IConfiguration cfg) =>
{
    // Dev-only auth: accept any username/password
    if (string.IsNullOrWhiteSpace(req.UserId))
        return Results.BadRequest("UserId required.");

    var key = cfg["JWT_KEY"] ?? "dev_super_secret_key_change_me_please_32chars";
    var issuer = cfg["JWT_ISSUER"] ?? "ShopSphere.Identity";
    var audience = cfg["JWT_AUDIENCE"] ?? "ShopSphere";

    var claims = new List<Claim>
    {
        new(JwtRegisteredClaimNames.Sub, req.UserId),
        new("role", req.Role ?? "user")
    };

    var creds = new SigningCredentials(
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: issuer,
        audience: audience,
        claims: claims,
        expires: DateTime.UtcNow.AddHours(4),
        signingCredentials: creds);

    var jwt = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { access_token = jwt, token_type = "Bearer", expires_in = 14400 });
})
.WithName("IssueToken");

app.Run();

public sealed record TokenRequest(string UserId, string? Role, string? Password);
