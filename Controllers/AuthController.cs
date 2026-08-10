using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(
    WarehouseDbContext db,
    IPasswordHasher<AppUser> passwordHasher,
    JwtTokenService tokenService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("register-tenant")]
    public async Task<ActionResult<RegisterTenantResponse>> RegisterTenant(
        RegisterTenantRequest request, CancellationToken cancellationToken)
    {
        var username = request.Username.Trim();
        var normalizedEmail = request.OwnerEmail.Trim().ToUpperInvariant();

        if (await db.Users.AnyAsync(user =>
                user.Username.ToUpper() == username.ToUpper() ||
                user.NormalizedEmail == normalizedEmail, cancellationToken))
            return Conflict(new { message = "Username or email is already in use." });

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        var tenant = new Client
        {
            CompanyName = request.CompanyName.Trim(),
            ContactName = request.ContactName?.Trim(),
            Email = request.CompanyEmail?.Trim(),
            Phone = request.Phone?.Trim(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        db.Clients.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);

        var owner = new AppUser
        {
            ClientId = tenant.Id,
            Username = username,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.OwnerEmail.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = UserRoles.TenantOwner,
            IsActive = true
        };
        owner.PasswordHash = passwordHasher.HashPassword(owner, request.Password);
        db.Users.Add(owner);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Created($"/api/clients/{tenant.Id}", new RegisterTenantResponse(
            tenant.Id, tenant.CompanyName, tokenService.Create(owner)));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request, CancellationToken cancellationToken)
    {
        var login = request.UsernameOrEmail.Trim().ToUpperInvariant();
        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Username.ToUpper() == login ||
                         candidate.NormalizedEmail == login,
            cancellationToken);

        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid username or password." });

        if (user.LockedUntil is DateTime lockedUntil && lockedUntil > DateTime.UtcNow)
            return Unauthorized(new { message = "Account is temporarily locked." });

        var result = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
                user.LockedUntil = DateTime.UtcNow.AddMinutes(15);
            await db.SaveChangesAsync(cancellationToken);
            return Unauthorized(new { message = "Invalid username or password." });
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return Ok(tokenService.Create(user));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUser>> Me(CancellationToken cancellationToken)
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idValue, out var userId))
            return Unauthorized();

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(
            candidate => candidate.Id == userId && candidate.IsActive,
            cancellationToken);
        if (user is null)
            return Unauthorized();

        return Ok(new AuthenticatedUser(
            user.Id, user.ClientId, user.Username, user.FirstName,
            user.LastName, user.Email, user.Role));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var idValue = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(idValue, out var userId))
            return Unauthorized();

        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId && candidate.IsActive,
            cancellationToken);
        if (user is null)
            return Unauthorized();

        var verification = passwordHasher.VerifyHashedPassword(
            user, user.PasswordHash, request.CurrentPassword);
        if (verification == PasswordVerificationResult.Failed)
            return BadRequest(new { message = "Current password is incorrect." });

        if (request.CurrentPassword == request.NewPassword)
            return BadRequest(new { message = "New password must be different." });

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockedUntil = null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.TenantOwner)]
    [HttpPost("users")]
    public async Task<ActionResult<AuthenticatedUser>> CreateUser(
        CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (!UserRoles.All.Contains(request.Role))
            return BadRequest(new { message = "Unknown user role." });

        var callerRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var callerTenant = User.FindFirst("tenant_id")?.Value;

        int? tenantId = request.TenantId;
        if (callerRole == UserRoles.TenantOwner)
        {
            if (!int.TryParse(callerTenant, out var resolvedTenantId))
                return Forbid();
            tenantId = resolvedTenantId;

            if (request.Role is UserRoles.SuperAdmin or UserRoles.TenantOwner)
                return Forbid();
        }

        if (request.Role is not (UserRoles.SuperAdmin or UserRoles.Bearer) && tenantId is null)
            return BadRequest(new { message = "A tenant is required for this role." });

        if (request.Role is UserRoles.SuperAdmin or UserRoles.Bearer)
            tenantId = null;

        if (tenantId is int id && !await db.Clients.AnyAsync(c => c.Id == id, cancellationToken))
            return BadRequest(new { message = "Tenant does not exist." });

        var username = request.Username.Trim();
        var normalizedEmail = request.Email?.Trim().ToUpperInvariant();
        if (await db.Users.AnyAsync(u => u.Username.ToUpper() == username.ToUpper() ||
            (normalizedEmail != null && u.NormalizedEmail == normalizedEmail), cancellationToken))
            return Conflict(new { message = "Username or email is already in use." });

        var user = new AppUser
        {
            ClientId = tenantId,
            Username = username,
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            Email = request.Email?.Trim(),
            NormalizedEmail = normalizedEmail,
            Role = request.Role,
            IsActive = true
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        return Created($"/api/users/{user.Id}", new AuthenticatedUser(
            user.Id, user.ClientId, user.Username, user.FirstName,
            user.LastName, user.Email, user.Role));
    }
}
