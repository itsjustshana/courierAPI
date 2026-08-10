using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;
using WarehouseApi.Services;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.TenantOwner)]
[Route("api/tenant/packages")]
public sealed class TenantPackagesController(WarehouseDbContext db, InvoicePdfService invoicePdfService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<TenantPackageResponse>>> GetPackages(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!int.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Forbid();
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var query = db.UserPackages.AsNoTracking()
            .Where(package => package.Assignment != null && package.Assignment.ClientId == tenantId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(package => package.Status == status.Trim());
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package => package.PackageNumber.Contains(term) ||
                (package.TrackingId != null && package.TrackingId.Contains(term)) ||
                (package.FullName != null && package.FullName.Contains(term)) ||
                (package.BatchItem != null && package.BatchItem.Batch.BatchNumber.Contains(term)));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(package => package.Created ?? package.CreatedAt)
            .ThenByDescending(package => package.PackageId).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(package => new TenantPackageResponse(
                package.PackageId, package.PackageNumber, package.TrackingId, package.FullName,
                package.Description, package.Status, package.Weight,
                package.Assignment == null ? 0 : package.Assignment.InvoiceCost,
                package.CustomsCharges ?? 0,
                package.PaidDate == null ? package.AmountDue ?? package.InvoiceAmount ?? 0 : 0,
                package.PaidDate, package.Created,
                package.BatchItem == null ? null : package.BatchItem.Batch.BatchNumber))
            .ToListAsync(cancellationToken);
        return Ok(new PagedResponse<TenantPackageResponse>(items, page, pageSize, total,
            (int)Math.Ceiling(total / (double)pageSize)));
    }

    [HttpGet("{packageId:int}/invoice")]
    public async Task<IActionResult> DownloadInvoice(int packageId, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Forbid();
        var package = await db.UserPackages.AsNoTracking()
            .Include(item => item.Assignment)!.ThenInclude(assignment => assignment!.Client)
            .Include(item => item.Assignment)!.ThenInclude(assignment => assignment!.User)
            .SingleOrDefaultAsync(item => item.PackageId == packageId && item.Assignment != null && item.Assignment.ClientId == tenantId, cancellationToken);
        if (package?.Assignment is null) return NotFound(new { message = "Package does not belong to this tenant." });
        var pdf = await invoicePdfService.GenerateAsync(package, package.Assignment, package.Assignment.Client, cancellationToken);
        return File(pdf, "application/pdf", $"invoice-{package.PackageNumber}.pdf");
    }
}
