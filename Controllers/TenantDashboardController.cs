using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.TenantOwner)]
[Route("apicour/tenant/dashboard")]
public sealed class TenantDashboardController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TenantDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Forbid();
        var tenant = await db.Clients.AsNoTracking().SingleOrDefaultAsync(client => client.Id == tenantId && client.IsActive, cancellationToken);
        if (tenant is null) return NotFound(new { message = "Tenant does not exist or is inactive." });

        var packages = db.UserPackages.AsNoTracking()
            .Where(package => package.Assignment != null && package.Assignment.ClientId == tenantId);
        var statusCounts = await packages.Where(package => package.Status != null && package.Status != "")
            .GroupBy(package => package.Status!)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);
        var financials = await packages.GroupBy(_ => 1).Select(group => new
        {
            Invoice = group.Sum(package => package.Assignment == null ? 0 : package.Assignment.InvoiceCost),
            Due = group.Sum(package => package.PaidDate == null ? package.AmountDue ?? package.InvoiceAmount ?? 0 : 0)
        }).FirstOrDefaultAsync(cancellationToken);
        var recent = await packages.OrderByDescending(package => package.Created ?? package.CreatedAt)
            .ThenByDescending(package => package.PackageId).Take(10)
            .Select(package => new TenantRecentPackageResponse(
                package.PackageId, package.PackageNumber, package.TrackingId, package.FullName,
                package.Status, package.Weight,
                package.Assignment == null ? 0 : package.Assignment.InvoiceCost,
                package.PaidDate == null ? package.AmountDue ?? package.InvoiceAmount ?? 0 : 0,
                package.Created,
                package.BatchItem == null ? null : package.BatchItem.Batch.BatchNumber))
            .ToListAsync(cancellationToken);

        return Ok(new TenantDashboardResponse(
            tenant.Id, tenant.CompanyName, tenant.LogoUrl, tenant.BatchHandlingMode,
            tenant.PerLbRate, tenant.DefaultDeliveryFee,
            await packages.CountAsync(cancellationToken),
            await db.Users.CountAsync(user => user.ClientId == tenantId && user.IsActive, cancellationToken),
            financials?.Invoice ?? 0, financials?.Due ?? 0, statusCounts, recent));
    }
}
