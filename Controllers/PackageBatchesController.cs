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
[Authorize(Roles = UserRoles.SuperAdmin)]
[Route("api/admin/package-batches")]
public sealed class PackageBatchesController(WarehouseDbContext db, InvoicePdfService invoicePdfService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PackageBatchResponse>>> GetBatches(
        [FromQuery] int? clientId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = db.PackageBatches.AsNoTracking().AsQueryable();
        if (clientId.HasValue) query = query.Where(batch => batch.ClientId == clientId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(batch => batch.Status == status.Trim());

        return Ok(await query.OrderByDescending(batch => batch.CreatedAt)
            .Take(200)
            .Select(batch => new PackageBatchResponse(
                batch.Id, batch.BatchNumber, batch.ClientId, batch.Client.CompanyName,
                batch.FulfillmentMethod, batch.Status, batch.ScheduledDate,
                batch.CompletedDate, batch.Items.Count, batch.CreatedAt, batch.Notes,
                batch.DeliveryFee, batch.DeliveryArea, batch.DeliveryAddress,
                batch.DeliveryFeeSource, batch.DeliveryFeeOverrideReason,
                batch.DeliveryFee + batch.Items.Sum(item => item.Package.Assignment == null ? 0 : item.Package.Assignment.InvoiceCost),
                batch.PaidDate == null ? batch.DeliveryFee + batch.Items.Sum(item => item.Package.Assignment == null ? 0 : item.Package.Assignment.InvoiceCost) : 0,
                batch.PaidDate))
            .ToListAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<PackageBatchResponse>> CreateBatch(
        CreatePackageBatchRequest request,
        CancellationToken cancellationToken)
    {
        var packageIds = request.PackageIds.Distinct().ToArray();
        if (packageIds.Length < 2)
            return BadRequest(new { message = "Select at least two packages for a batch." });
        if (packageIds.Length > 500)
            return BadRequest(new { message = "A batch cannot contain more than 500 packages." });

        var method = request.FulfillmentMethod.Trim();
        var resolvedMethod = new[] { "Delivery", "Pickup" }.FirstOrDefault(item =>
            item.Equals(method, StringComparison.OrdinalIgnoreCase));
        if (resolvedMethod is null)
            return BadRequest(new { message = "Fulfillment method must be Delivery or Pickup." });

        var assignments = await db.UserPackageAssignments.AsNoTracking()
            .Where(assignment => packageIds.Contains(assignment.PackageId))
            .ToListAsync(cancellationToken);
        if (assignments.Count != packageIds.Length)
            return BadRequest(new { message = "Every selected package must be assigned to a client." });
        var clientIds = assignments.Select(assignment => assignment.ClientId).Distinct().ToArray();
        if (clientIds.Length != 1)
            return BadRequest(new { message = "All packages in a batch must belong to the same client." });

        var client = await db.Clients.SingleAsync(item => item.Id == clientIds[0], cancellationToken);
        var modeAllowsMethod = client.BatchHandlingMode.Equals("Both", StringComparison.OrdinalIgnoreCase) ||
            client.BatchHandlingMode.Equals(resolvedMethod, StringComparison.OrdinalIgnoreCase);
        if (!modeAllowsMethod)
            return BadRequest(new { message = $"{client.CompanyName} is not enabled for batch {resolvedMethod.ToLowerInvariant()}." });
        if (request.DeliveryFee is < 0 or > 99_999_999.99m)
            return BadRequest(new { message = "Delivery fee must be within the allowed range." });

        if (await db.PackageBatchItems.AnyAsync(item => packageIds.Contains(item.PackageId), cancellationToken))
            return Conflict(new { message = "One or more selected packages already belong to a batch." });

        var actorClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(actorClaim, out var actorId)) return Unauthorized();

        var defaultFee = resolvedMethod == "Pickup" ? 0 : client.DefaultDeliveryFee;
        var deliveryFee = request.DeliveryFee.HasValue ? decimal.Round(request.DeliveryFee.Value, 2) : defaultFee;
        var feeSource = request.DeliveryFee.HasValue && deliveryFee != defaultFee ? "Manual" :
            resolvedMethod == "Pickup" ? "Pickup" : "ClientDefault";
        var batch = new PackageBatch
        {
            ClientId = client.Id,
            BatchNumber = $"BAT-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            FulfillmentMethod = resolvedMethod,
            DeliveryFee = deliveryFee,
            DeliveryArea = Clean(request.DeliveryArea, 100),
            DeliveryAddress = Clean(request.DeliveryAddress, 255),
            DeliveryFeeSource = feeSource,
            DeliveryFeeOverrideReason = feeSource == "Manual" ? Clean(request.DeliveryFeeOverrideReason, 255) : null,
            Status = "Draft",
            ScheduledDate = request.ScheduledDate?.Date,
            CreatedByUserId = actorId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()
        };
        foreach (var packageId in packageIds)
            batch.Items.Add(new PackageBatchItem { PackageId = packageId });

        db.PackageBatches.Add(batch);
        await db.SaveChangesAsync(cancellationToken);

        var invoiceTotal = deliveryFee + assignments.Sum(assignment => assignment.InvoiceCost);

        return Created($"/api/admin/package-batches/{batch.Id}", new PackageBatchResponse(
            batch.Id, batch.BatchNumber, batch.ClientId, client.CompanyName,
            batch.FulfillmentMethod, batch.Status, batch.ScheduledDate,
            batch.CompletedDate, batch.Items.Count, batch.CreatedAt, batch.Notes,
            batch.DeliveryFee, batch.DeliveryArea, batch.DeliveryAddress,
            batch.DeliveryFeeSource, batch.DeliveryFeeOverrideReason,
            invoiceTotal, invoiceTotal, batch.PaidDate));
    }

    [HttpPut("{batchId:int}/paid-date")]
    public async Task<IActionResult> UpdatePaidDate(
        int batchId,
        UpdateBatchPaidDateRequest request,
        CancellationToken cancellationToken)
    {
        var batch = await db.PackageBatches.SingleOrDefaultAsync(item => item.Id == batchId, cancellationToken);
        if (batch is null) return NotFound(new { message = "Batch does not exist." });
        batch.PaidDate = request.PaidDate?.Date;
        batch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{batchId:int}/invoice")]
    public async Task<IActionResult> DownloadInvoice(int batchId, CancellationToken cancellationToken)
    {
        var batch = await db.PackageBatches.AsNoTracking()
            .Include(item => item.Client)
            .Include(item => item.Items).ThenInclude(item => item.Package).ThenInclude(package => package.Assignment)
            .SingleOrDefaultAsync(item => item.Id == batchId, cancellationToken);
        if (batch is null) return NotFound(new { message = "Batch does not exist." });
        if (batch.Items.Count == 0) return BadRequest(new { message = "An empty batch cannot produce an invoice." });
        var globalSettings = await db.GlobalSettings.AsNoTracking()
            .OrderBy(item => item.Id)
            .FirstOrDefaultAsync(cancellationToken);
        var pdf = await invoicePdfService.GenerateBatchAsync(
            batch,
            globalSettings?.AppName ?? "MekMiCourier",
            globalSettings?.LogoUrl,
            cancellationToken);
        return File(pdf, "application/pdf", $"batch-invoice-{batch.BatchNumber}.pdf");
    }

    [HttpPut("{batchId:int}/delivery-fee")]
    public async Task<IActionResult> UpdateDeliveryFee(
        int batchId,
        UpdateBatchDeliveryFeeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DeliveryFee < 0 || request.DeliveryFee > 99_999_999.99m)
            return BadRequest(new { message = "Delivery fee must be within the allowed range." });
        var batch = await db.PackageBatches.Include(item => item.Client)
            .SingleOrDefaultAsync(item => item.Id == batchId, cancellationToken);
        if (batch is null) return NotFound(new { message = "Batch does not exist." });
        var fee = decimal.Round(request.DeliveryFee, 2);
        batch.DeliveryFee = fee;
        batch.DeliveryArea = Clean(request.DeliveryArea, 100);
        batch.DeliveryAddress = Clean(request.DeliveryAddress, 255);
        batch.DeliveryFeeSource = fee == batch.Client.DefaultDeliveryFee ? "ClientDefault" : "Manual";
        batch.DeliveryFeeOverrideReason = batch.DeliveryFeeSource == "Manual" ? Clean(request.OverrideReason, 255) : null;
        batch.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{batchId:int}/packages")]
    public async Task<ActionResult<IReadOnlyList<BatchPackageResponse>>> GetBatchPackages(
        int batchId,
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!await db.PackageBatches.AsNoTracking().AnyAsync(batch => batch.Id == batchId, cancellationToken))
            return NotFound(new { message = "Batch does not exist." });

        var query = db.PackageBatchItems.AsNoTracking()
            .Where(item => item.BatchId == batchId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item =>
                item.Package.PackageNumber.Contains(term) ||
                (item.Package.TrackingId != null && item.Package.TrackingId.Contains(term)) ||
                (item.Package.FullName != null && item.Package.FullName.Contains(term)));
        }

        return Ok(await query
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new BatchPackageResponse(
                item.PackageId,
                item.Package.PackageNumber,
                item.Package.TrackingId,
                item.Package.FullName,
                item.Package.Description,
                item.Package.Status,
                item.Package.Weight,
                item.AddedAt,
                item.Package.Status == null ||
                    (item.Package.Status.ToLower() != "collected" && item.Package.Status.ToLower() != "delivered")))
            .ToListAsync(cancellationToken));
    }

    [HttpDelete("{batchId:int}/packages/{packageId:int}")]
    public async Task<IActionResult> RemovePackage(
        int batchId,
        int packageId,
        CancellationToken cancellationToken)
    {
        var item = await db.PackageBatchItems
            .Include(batchItem => batchItem.Package)
            .SingleOrDefaultAsync(batchItem =>
                batchItem.BatchId == batchId && batchItem.PackageId == packageId,
                cancellationToken);
        if (item is null)
            return NotFound(new { message = "Package does not belong to this batch." });

        if (item.Package.Status is not null &&
            (item.Package.Status.Equals("Collected", StringComparison.OrdinalIgnoreCase) ||
             item.Package.Status.Equals("Delivered", StringComparison.OrdinalIgnoreCase)))
            return Conflict(new { message = "Collected or delivered packages cannot be removed from a batch." });

        db.PackageBatchItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("{batchId:int}/status")]
    public async Task<IActionResult> UpdateStatus(
        int batchId,
        UpdatePackageBatchStatusRequest request,
        CancellationToken cancellationToken)
    {
        var statuses = new[] { "Draft", "Ready", "Scheduled", "OutForDelivery", "ReadyForPickup", "Collected", "Delivered", "Cancelled" };
        var status = statuses.FirstOrDefault(item => item.Equals(request.Status.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null) return BadRequest(new { message = "Unknown batch status." });

        var batch = await db.PackageBatches.SingleOrDefaultAsync(item => item.Id == batchId, cancellationToken);
        if (batch is null) return NotFound(new { message = "Batch does not exist." });
        batch.Status = status;
        batch.CompletedDate = status is "Collected" or "Delivered" ? DateTime.UtcNow.Date : null;
        batch.UpdatedAt = DateTime.UtcNow;
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
