using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.SuperAdmin)]
[Route("api/admin/users")]
public sealed class AdminUsersController(WarehouseDbContext db, IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminUserResponse>>> GetUsers(
        [FromQuery] int? tenantId,
        CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (tenantId.HasValue) query = query.Where(user => user.ClientId == tenantId);
        return Ok(await query.OrderBy(user => user.FirstName).ThenBy(user => user.LastName).ThenBy(user => user.Username)
            .Select(user => new AdminUserResponse(user.Id, user.ClientId,
                user.Client == null ? null : user.Client.CompanyName, user.Username,
                user.FirstName, user.LastName, user.Email, user.Role, user.IsActive,
                user.CreatedAt, user.LastLoginAt))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AdminUserResponse>> CreateUser(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100)
            return BadRequest(new { message = "Username is required and cannot exceed 100 characters." });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 10 || request.Password.Length > 200)
            return BadRequest(new { message = "Password must be between 10 and 200 characters." });
        if (!UserRoles.All.Contains(request.Role)) return BadRequest(new { message = "Unknown user role." });
        var platformRole = request.Role is UserRoles.SuperAdmin or UserRoles.Bearer;
        var tenantId = platformRole ? null : request.TenantId;
        if (!platformRole && tenantId is null)
            return BadRequest(new { message = "A tenant is required for this role." });
        Client? tenant = null;
        if (tenantId.HasValue)
        {
            tenant = await db.Clients.SingleOrDefaultAsync(client => client.Id == tenantId && client.IsActive, cancellationToken);
            if (tenant is null) return BadRequest(new { message = "Tenant does not exist or is inactive." });
        }
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedEmail = email?.ToUpperInvariant();
        if (await db.Users.AnyAsync(user => user.Username.ToUpper() == username.ToUpper() ||
            (normalizedEmail != null && user.NormalizedEmail == normalizedEmail), cancellationToken))
            return Conflict(new { message = "Username or email is already in use." });

        var user = new AppUser
        {
            ClientId = tenantId,
            Username = username,
            FirstName = Clean(request.FirstName, 100),
            LastName = Clean(request.LastName, 100),
            Email = email,
            NormalizedEmail = normalizedEmail,
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/api/admin/users/{user.Id}", new AdminUserResponse(
            user.Id, user.ClientId, tenant?.CompanyName, user.Username, user.FirstName,
            user.LastName, user.Email, user.Role, user.IsActive, user.CreatedAt, null));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(
        int id,
        UpdateAdminUserRequest request,
        CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim();
        if (string.IsNullOrWhiteSpace(username) || username.Length > 100)
            return BadRequest(new { message = "Username is required and cannot exceed 100 characters." });
        if (!UserRoles.All.Contains(request.Role)) return BadRequest(new { message = "Unknown user role." });
        var platformRole = request.Role is UserRoles.SuperAdmin or UserRoles.Bearer;
        var tenantId = platformRole ? null : request.TenantId;
        if (!platformRole && tenantId is null)
            return BadRequest(new { message = "A tenant is required for this role." });
        if (tenantId.HasValue && !await db.Clients.AnyAsync(client => client.Id == tenantId, cancellationToken))
            return BadRequest(new { message = "Tenant does not exist." });

        var user = await db.Users.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound(new { message = "User does not exist." });
        var currentId = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actorId) ? actorId : 0;
        if (currentId == id && (request.Role != UserRoles.SuperAdmin || !request.IsActive))
            return BadRequest(new { message = "You cannot remove your own SuperAdmin access or deactivate your account." });
        var email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        var normalizedEmail = email?.ToUpperInvariant();
        if (await db.Users.AnyAsync(item => item.Id != id &&
            (item.Username.ToUpper() == username.ToUpper() ||
             (normalizedEmail != null && item.NormalizedEmail == normalizedEmail)), cancellationToken))
            return Conflict(new { message = "Username or email is already in use." });

        user.ClientId = tenantId;
        user.Username = username;
        user.FirstName = Clean(request.FirstName, 100);
        user.LastName = Clean(request.LastName, 100);
        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        if (user.IsActive) { user.FailedLoginAttempts = 0; user.LockedUntil = null; }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
