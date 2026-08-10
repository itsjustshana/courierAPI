using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.Customer)]
[Route("api/customer")]
public sealed class CustomerPortalController(WarehouseDbContext db, InvoicePdfService invoicePdfService) : ControllerBase
{
    [HttpGet("profile")]
    public async Task<ActionResult<CustomerProfileResponse>> GetProfile(CancellationToken cancellationToken)
    {
        if (!ContextIds(out var userId, out var tenantId)) return Forbid();
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(item =>
            item.Id == userId && item.ClientId == tenantId && item.IsActive, cancellationToken);
        if (user is null) return NotFound();
        return Ok(new CustomerProfileResponse(user.Username, user.Email, user.FirstName,
            user.LastName, user.FullName, user.Mobile, user.HomePhone, user.IdType,
            user.IdNumber, user.PickupLocation, user.Address1, user.Address2,
            user.City, user.Parish));
    }

    [HttpGet("dashboard")]
    public async Task<ActionResult<CustomerDashboardResponse>> GetDashboard(CancellationToken cancellationToken)
    {
        if (!ContextIds(out var userId, out var tenantId)) return Forbid();
        var tenant = await db.Clients.AsNoTracking().SingleOrDefaultAsync(item => item.Id == tenantId && item.IsActive, cancellationToken);
        if (tenant is null) return NotFound(new { message = "Courier account is unavailable." });
        var packages = db.UserPackages.AsNoTracking().Where(item => item.Assignment != null && item.Assignment.ClientId == tenantId && item.Assignment.UserId == userId);
        var counts = await packages.Where(item => item.Status != null && item.Status != "").GroupBy(item => item.Status!)
            .Select(group => new { group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var totals = await packages.GroupBy(_ => 1).Select(group => new
        {
            Invoice = group.Sum(item => item.Assignment == null ? 0 :
                (item.Weight ?? 0) * (item.Assignment.PerLbCost + item.Assignment.PerLbMarkup) + (item.CustomsCharges ?? 0)),
            Due = group.Sum(item => item.PaidDate == null && item.Assignment != null
                ? (item.Weight ?? 0) * (item.Assignment.PerLbCost + item.Assignment.PerLbMarkup) + (item.CustomsCharges ?? 0)
                : 0)
        }).FirstOrDefaultAsync(cancellationToken);
        return Ok(new CustomerDashboardResponse(tenant.CompanyName, tenant.LogoUrl,
            await packages.CountAsync(cancellationToken), totals?.Invoice ?? 0, totals?.Due ?? 0, counts));
    }

    [HttpGet("packages")]
    public async Task<ActionResult<PagedResponse<TenantPackageResponse>>> GetPackages(
        [FromQuery] string? search, [FromQuery] string? status, [FromQuery] bool unpaidOnly = false, [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default)
    {
        if (!ContextIds(out var userId, out var tenantId)) return Forbid();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var query = db.UserPackages.AsNoTracking().Where(item => item.Assignment != null && item.Assignment.ClientId == tenantId && item.Assignment.UserId == userId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(item => item.Status == status.Trim());
        if (unpaidOnly) query = query.Where(item => item.PaidDate == null && item.Assignment != null &&
            ((item.Weight ?? 0) * (item.Assignment.PerLbCost + item.Assignment.PerLbMarkup) + (item.CustomsCharges ?? 0)) > 0);
        if (!string.IsNullOrWhiteSpace(search)) { var term = search.Trim(); query = query.Where(item => item.PackageNumber.Contains(term) || (item.TrackingId != null && item.TrackingId.Contains(term)) || (item.Description != null && item.Description.Contains(term))); }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(item => item.Created ?? item.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(item => new TenantPackageResponse(item.PackageId, item.PackageNumber, item.TrackingId, item.FullName,
                item.Description, item.Status, item.Weight, item.Assignment == null ? 0 :
                    (item.Weight ?? 0) * (item.Assignment.PerLbCost + item.Assignment.PerLbMarkup) + (item.CustomsCharges ?? 0),
                item.CustomsCharges ?? 0, item.PaidDate == null && item.Assignment != null
                    ? (item.Weight ?? 0) * (item.Assignment.PerLbCost + item.Assignment.PerLbMarkup) + (item.CustomsCharges ?? 0)
                    : 0,
                item.PaidDate, item.Created, null))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<TenantPackageResponse>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize)));
    }

    [HttpGet("packages/{packageId:int}/invoice")]
    public async Task<IActionResult> DownloadInvoice(int packageId, CancellationToken cancellationToken)
    {
        if (!ContextIds(out var userId, out var tenantId)) return Forbid();
        var package = await db.UserPackages.AsNoTracking().Include(item => item.Assignment)!.ThenInclude(item => item!.Client)
            .Include(item => item.Assignment)!.ThenInclude(item => item!.User)
            .SingleOrDefaultAsync(item => item.PackageId == packageId && item.Assignment != null && item.Assignment.ClientId == tenantId && item.Assignment.UserId == userId, cancellationToken);
        if (package?.Assignment is null) return NotFound();
        var pdf = await invoicePdfService.GenerateAsync(package, package.Assignment, package.Assignment.Client, cancellationToken);
        return File(pdf, "application/pdf", $"invoice-{package.PackageNumber}.pdf");
    }

    private bool ContextIds(out int userId, out int tenantId)
    {
        var hasUser = int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
        var hasTenant = int.TryParse(User.FindFirst("tenant_id")?.Value, out tenantId);
        return hasUser && hasTenant;
    }
}
