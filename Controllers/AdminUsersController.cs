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
[Route("apicour/admin/users")]
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
        return Created($"/apicour/admin/users/{user.Id}", new AdminUserResponse(
            user.Id, user.ClientId, tenant?.CompanyName, user.Username, user.FirstName,
            user.LastName, user.Email, user.Role, user.IsActive, user.CreatedAt, null));
    }

    [HttpGet("{id:int}/dashboard-preview")]
    public async Task<ActionResult<UserDashboardPreviewResponse>> GetDashboardPreview(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Include(item => item.Client).SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (user is null) return NotFound(new { message = "User does not exist." });
        var packages = db.UserPackages.AsNoTracking().AsQueryable();
        if (user.Role == UserRoles.Customer) packages = packages.Where(item => item.Assignment != null && item.Assignment.UserId == id);
        else if (user.ClientId is int tenantId) packages = packages.Where(item => item.Assignment != null && item.Assignment.ClientId == tenantId);
        else if (user.Role == UserRoles.Bearer) packages = packages.Where(item => item.SupplierCollectionItem != null && item.SupplierCollectionItem.Collection.BearerUserId == id);
        var totals = await packages.GroupBy(_ => 1).Select(group => new { Count=group.Count(), Invoice=group.Sum(item=>item.InvoiceAmount??0), Due=group.Sum(item=>item.PaidDate==null?item.AmountDue??0:0) }).FirstOrDefaultAsync(cancellationToken);
        var counts = await packages.Where(item=>item.Status!=null&&item.Status!="").GroupBy(item=>item.Status!).Select(group=>new{group.Key,Count=group.Count()}).ToDictionaryAsync(item=>item.Key,item=>item.Count,cancellationToken);
        var recent = await packages.OrderByDescending(item=>item.Created??item.CreatedAt).ThenByDescending(item=>item.PackageId).Take(20).Select(item=>new UserDashboardPreviewPackage(item.PackageId,item.PackageNumber,item.TrackingId,item.FullName,item.Status,item.Weight,item.InvoiceAmount,item.AmountDue,item.PaidDate,item.Created??item.CreatedAt)).ToListAsync(cancellationToken);
        var collections=db.SupplierCollections.AsNoTracking().Where(item=>item.BearerUserId==id);
        var totalCollections=user.Role==UserRoles.Bearer?await collections.CountAsync(cancellationToken):0;
        var openCollections=user.Role==UserRoles.Bearer?await collections.CountAsync(item=>item.Status=="Open",cancellationToken):0;
        var name=$"{user.FirstName} {user.LastName}".Trim();
        return Ok(new UserDashboardPreviewResponse(user.Id,string.IsNullOrWhiteSpace(name)?user.Username:name,user.Username,user.Role,user.Client?.CompanyName,user.IsActive,totals?.Count??0,totals?.Invoice??0,totals?.Due??0,totalCollections,openCollections,counts,recent));
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
