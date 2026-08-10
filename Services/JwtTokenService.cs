using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Services;

public sealed class JwtTokenService(IConfiguration configuration)
{
    public LoginResponse Create(AppUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(8);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.ClientId is int tenantId)
            claims.Add(new Claim("tenant_id", tenantId.ToString()));

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT key is missing.");
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: configuration["Jwt:Issuer"],
            audience: configuration["Jwt:Audience"],
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new LoginResponse(
            new JwtSecurityTokenHandler().WriteToken(token),
            expiresAt,
            new AuthenticatedUser(
                user.Id, user.ClientId, user.Username, user.FirstName,
                user.LastName, user.Email, user.Role));
    }
}
