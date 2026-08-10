using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.TenantOwner)]
[Route("api/tenant/batches")]
public sealed class TenantBatchesController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PackageBatchResponse>>> GetBatches(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] string? payment,
        CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Forbid();
        var query = db.PackageBatches.AsNoTracking().Where(batch => batch.ClientId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(batch => batch.BatchNumber.Contains(term) ||
                (batch.DeliveryArea != null && batch.DeliveryArea.Contains(term)) ||
                batch.Items.Any(item => item.Package.PackageNumber.Contains(term) ||
                    (item.Package.TrackingId != null && item.Package.TrackingId.Contains(term))));
        }
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(batch => batch.Status == status.Trim());
        if (payment?.Equals("paid", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(batch => batch.PaidDate != null);
        if (payment?.Equals("unpaid", StringComparison.OrdinalIgnoreCase) == true) query = query.Where(batch => batch.PaidDate == null);

        return Ok(await query.OrderByDescending(batch => batch.CreatedAt).Take(200)
            .Select(batch => new PackageBatchResponse(
                batch.Id, batch.BatchNumber, batch.ClientId, batch.Client.CompanyName,
                batch.FulfillmentMethod, batch.Status, batch.ScheduledDate, batch.CompletedDate,
                batch.Items.Count, batch.CreatedAt, batch.Notes, batch.DeliveryFee,
                batch.DeliveryArea, batch.DeliveryAddress, batch.DeliveryFeeSource,
                batch.DeliveryFeeOverrideReason,
                batch.DeliveryFee + batch.Items.Sum(item => item.Package.Assignment == null ? 0 : item.Package.Assignment.InvoiceCost),
                batch.PaidDate == null ? batch.DeliveryFee + batch.Items.Sum(item => item.Package.Assignment == null ? 0 : item.Package.Assignment.InvoiceCost) : 0,
                batch.PaidDate))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("{batchId:int}/packages")]
    public async Task<ActionResult<IReadOnlyList<BatchPackageResponse>>> GetPackages(
        int batchId, [FromQuery] string? search, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst("tenant_id")?.Value, out var tenantId)) return Forbid();
        if (!await db.PackageBatches.AsNoTracking().AnyAsync(batch => batch.Id == batchId && batch.ClientId == tenantId, cancellationToken))
            return NotFound(new { message = "Batch does not belong to this tenant." });

        var query = db.PackageBatchItems.AsNoTracking().Where(item => item.BatchId == batchId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.Package.PackageNumber.Contains(term) ||
                (item.Package.TrackingId != null && item.Package.TrackingId.Contains(term)) ||
                (item.Package.FullName != null && item.Package.FullName.Contains(term)));
        }
        return Ok(await query.OrderByDescending(item => item.AddedAt)
            .Select(item => new BatchPackageResponse(item.PackageId, item.Package.PackageNumber,
                item.Package.TrackingId, item.Package.FullName, item.Package.Description,
                item.Package.Status, item.Package.Weight, item.AddedAt, false))
            .ToListAsync(cancellationToken));
    }
}
