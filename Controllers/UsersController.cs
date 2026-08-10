using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.TenantOwner)]
[Route("api/users")]
public sealed class UsersController(
    WarehouseDbContext db,
    IPasswordHasher<AppUser> passwordHasher) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AuthenticatedUser>>> GetUsers(
        [FromQuery] int? tenantId, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking().AsQueryable();
        if (!TryApplyTenantScope(ref query, tenantId, out var error))
            return error!;

        var users = await query
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new AuthenticatedUser(
                user.Id, user.ClientId, user.Username, user.FirstName,
                user.LastName, user.Email, user.Role))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<AuthenticatedUser>> GetUser(
        int id, CancellationToken cancellationToken)
    {
        var user = await FindPermittedUser(id, cancellationToken);
        if (user is null)
            return NotFound();

        return Ok(new AuthenticatedUser(
            user.Id, user.ClientId, user.Username, user.FirstName,
            user.LastName, user.Email, user.Role));
    }

    [HttpPatch("{id:int}/active")]
    public async Task<IActionResult> SetActive(
        int id, SetUserActiveRequest request, CancellationToken cancellationToken)
    {
        var user = await FindPermittedUser(id, cancellationToken);
        if (user is null)
            return NotFound();

        if (CurrentUserId() == user.Id && !request.IsActive)
            return BadRequest(new { message = "You cannot deactivate your own account." });

        if (User.IsInRole(UserRoles.TenantOwner) && user.Role == UserRoles.TenantOwner)
            return Forbid();

        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;
        if (request.IsActive)
        {
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
        }
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<IActionResult> ResetPassword(
        int id, ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await FindPermittedUser(id, cancellationToken);
        if (user is null)
            return NotFound();

        if (User.IsInRole(UserRoles.TenantOwner) && user.Role is
            UserRoles.TenantOwner or UserRoles.SuperAdmin)
            return Forbid();

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private bool TryApplyTenantScope(
        ref IQueryable<AppUser> query,
        int? requestedTenantId,
        out ActionResult? error)
    {
        error = null;
        if (User.IsInRole(UserRoles.SuperAdmin))
        {
            if (requestedTenantId is int tenantId)
                query = query.Where(user => user.ClientId == tenantId);
            return true;
        }

        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        if (!int.TryParse(tenantClaim, out var currentTenantId))
        {
            error = Forbid();
            return false;
        }

        query = query.Where(user => user.ClientId == currentTenantId);
        return true;
    }

    private async Task<AppUser?> FindPermittedUser(int id, CancellationToken cancellationToken)
    {
        var query = db.Users.Where(user => user.Id == id);
        if (User.IsInRole(UserRoles.SuperAdmin))
            return await query.SingleOrDefaultAsync(cancellationToken);

        var tenantClaim = User.FindFirst("tenant_id")?.Value;
        return int.TryParse(tenantClaim, out var tenantId)
            ? await query.SingleOrDefaultAsync(user => user.ClientId == tenantId, cancellationToken)
            : null;
    }

    private int? CurrentUserId() =>
        int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : null;
}
