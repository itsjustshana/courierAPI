using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;
using System.Security.Claims;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.SuperAdmin + "," + UserRoles.Bearer)]
[Route("apicour/admin/supplier-settlements")]
public sealed class SupplierSettlementsController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<ActionResult<SupplierSettlementPage>> Get(
        [FromQuery] string? search, [FromQuery] int? tenantId,
        [FromQuery] string payment = "all", [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page); pageSize = Math.Clamp(pageSize, 10, 100);
        var query = db.UserPackages.AsNoTracking().AsQueryable();
        if (tenantId.HasValue) query = query.Where(package => package.Assignment != null && package.Assignment.ClientId == tenantId);
        if (payment.Equals("paid", StringComparison.OrdinalIgnoreCase)) query = query.Where(package => package.SupplierPaidDate != null);
        else if (payment.Equals("unpaid", StringComparison.OrdinalIgnoreCase)) query = query.Where(package => package.SupplierAmount != null && package.SupplierAmount > 0 && package.SupplierPaidDate == null);
        else if (payment.Equals("missing", StringComparison.OrdinalIgnoreCase)) query = query.Where(package => package.SupplierAmount == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package => package.PackageNumber.Contains(term) ||
                (package.TrackingId != null && package.TrackingId.Contains(term)) ||
                (package.FullName != null && package.FullName.Contains(term)) ||
                (package.SupplierPaymentReference != null && package.SupplierPaymentReference.Contains(term)));
        }
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPayable = await query.SumAsync(package => package.SupplierAmount ?? 0, cancellationToken);
        var outstanding = await query.Where(package => package.SupplierPaidDate == null).SumAsync(package => package.SupplierAmount ?? 0, cancellationToken);
        var paid = await query.Where(package => package.SupplierPaidDate != null).SumAsync(package => package.SupplierAmount ?? 0, cancellationToken);
        var items = await query.OrderByDescending(package => package.Created ?? package.CreatedAt).ThenByDescending(package => package.PackageId)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(package => new SupplierSettlementResponse(
                package.PackageId, package.PackageNumber, package.TrackingId, package.FullName,
                package.Status, package.Assignment == null ? null : package.Assignment.ClientId,
                package.Assignment == null ? null : package.Assignment.Client.CompanyName,
                package.SupplierAmount, package.SupplierPaidDate, package.SupplierPaymentReference,
                package.SupplierCollectionItem == null ? null : package.SupplierCollectionItem.CollectionId,
                package.SupplierCollectionItem == null ? null : package.SupplierCollectionItem.Collection.CollectionNumber,
                package.Created))
            .ToListAsync(cancellationToken);
        return Ok(new SupplierSettlementPage(items, page, pageSize, totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize), totalPayable, outstanding, paid));
    }

    [HttpPut("{packageId:int}")]
    [Authorize(Roles = UserRoles.SuperAdmin)]
    public async Task<IActionResult> Update(
        int packageId, UpdateSupplierSettlementRequest request,
        CancellationToken cancellationToken)
    {
        if (request.SupplierAmount is < 0 or > 999_999_999.99m)
            return BadRequest(new { message = "Supplier amount must be within the allowed range." });
        var package = await db.UserPackages.SingleOrDefaultAsync(item => item.PackageId == packageId, cancellationToken);
        if (package is null) return NotFound(new { message = "Package does not exist." });
        package.SupplierAmount = request.SupplierAmount.HasValue ? decimal.Round(request.SupplierAmount.Value, 2) : null;
        package.SupplierPaidDate = request.PaidDate?.Date;
        package.SupplierPaymentReference = string.IsNullOrWhiteSpace(request.PaymentReference) ? null : request.PaymentReference.Trim()[..Math.Min(request.PaymentReference.Trim().Length, 100)];
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("collections")]
    public async Task<ActionResult<SupplierCollectionResponse>> CreateCollection(
        CreateSupplierCollectionRequest request, CancellationToken cancellationToken)
    {
        var supplier = await db.GlobalSettings.AsNoTracking().OrderBy(item => item.Id)
            .Select(item => item.Supplier).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(supplier)) return BadRequest(new { message = "Configure the global supplier before creating collections." });
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var actorId)) return Unauthorized();
        AppUser? bearer = null;
        var bearerUserId = User.IsInRole(UserRoles.Bearer) ? actorId : request.BearerUserId;
        if (bearerUserId.HasValue)
        {
            bearer = await db.Users.AsNoTracking().SingleOrDefaultAsync(user => user.Id == bearerUserId && user.IsActive, cancellationToken);
            if (bearer is null) return BadRequest(new { message = "The selected bearer is not available." });
        }
        var packageIds = request.PackageIds?.Distinct().ToArray() ?? [];
        if (packageIds.Length > 500) return BadRequest(new { message = "A collection cannot contain more than 500 packages." });
        if (packageIds.Length > 0)
        {
            if (await db.UserPackages.CountAsync(package => packageIds.Contains(package.PackageId), cancellationToken) != packageIds.Length)
                return BadRequest(new { message = "One or more selected packages do not exist." });
            if (await db.SupplierCollectionItems.AnyAsync(item => packageIds.Contains(item.PackageId), cancellationToken))
                return Conflict(new { message = "One or more selected packages already belong to a collection." });
        }
        var collection = new SupplierCollection
        {
            CollectionNumber = $"COL-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            SupplierName = supplier,
            BearerUserId = bearer?.Id,
            CreatedByUserId = actorId,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim()[..Math.Min(request.Notes.Trim().Length, 500)]
        };
        foreach (var packageId in packageIds) collection.Items.Add(new SupplierCollectionItem { PackageId = packageId });
        db.SupplierCollections.Add(collection);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/apicour/admin/supplier-settlements/collections/{collection.Id}",
            new SupplierCollectionResponse(collection.Id, collection.CollectionNumber, collection.SupplierName,
                bearer?.Id, bearer?.Username, collection.Status, packageIds.Length, collection.Notes, collection.CreatedAt, null));
    }

    [HttpGet("collections/uncollected")]
    public async Task<ActionResult<IReadOnlyList<CollectionAvailablePackageResponse>>> GetUncollectedPackages(
        [FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = db.UserPackages.AsNoTracking().Where(package => package.SupplierCollectionItem == null);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package => package.PackageNumber.Contains(term) ||
                (package.TrackingId != null && package.TrackingId.Contains(term)) ||
                (package.FullName != null && package.FullName.Contains(term)));
        }
        return Ok(await query.OrderByDescending(package => package.Created ?? package.CreatedAt).Take(500)
            .Select(package => new CollectionAvailablePackageResponse(package.PackageId, package.PackageNumber,
                package.TrackingId, package.FullName, package.Description, package.Status, package.Created))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("collections")]
    public async Task<ActionResult<IReadOnlyList<SupplierCollectionResponse>>> GetCollections(CancellationToken cancellationToken)
    {
        var query = db.SupplierCollections.AsNoTracking().AsQueryable();
        if (User.IsInRole(UserRoles.Bearer))
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var bearerId)) return Forbid();
            query = query.Where(collection => collection.BearerUserId == bearerId);
        }
        return Ok(await query.OrderByDescending(collection => collection.CreatedAt).Take(200)
            .Select(collection => new SupplierCollectionResponse(collection.Id, collection.CollectionNumber,
                collection.SupplierName, collection.BearerUserId,
                collection.BearerUser == null ? null : collection.BearerUser.Username,
                collection.Status, collection.Items.Count, collection.Notes, collection.CreatedAt, collection.CompletedAt))
            .ToListAsync(cancellationToken));
    }

    [HttpGet("collections/{collectionId:int}/packages")]
    public async Task<ActionResult<IReadOnlyList<BatchPackageResponse>>> GetCollectionPackages(int collectionId, CancellationToken cancellationToken)
    {
        if (!await AccessibleCollection(collectionId).AnyAsync(cancellationToken)) return NotFound();
        return Ok(await db.SupplierCollectionItems.AsNoTracking().Where(item => item.CollectionId == collectionId)
            .OrderByDescending(item => item.AddedAt)
            .Select(item => new BatchPackageResponse(item.PackageId, item.Package.PackageNumber, item.Package.TrackingId,
                item.Package.FullName, item.Package.Description, item.Package.Status, item.Package.Weight, item.AddedAt, false))
            .ToListAsync(cancellationToken));
    }

    [HttpPost("collections/{collectionId:int}/packages")]
    public async Task<IActionResult> AddCollectionPackage(int collectionId, AddCollectionPackageRequest request, CancellationToken cancellationToken)
    {
        var collection = await AccessibleCollection(collectionId).SingleOrDefaultAsync(cancellationToken);
        if (collection is null) return NotFound(new { message = "Collection does not exist." });
        if (collection.Status != "Open") return Conflict(new { message = "Only open collections can receive packages." });
        if (string.IsNullOrWhiteSpace(request.PackageNumber)) return BadRequest(new { message = "Package number is required." });
        var number = request.PackageNumber.Trim();
        var package = await db.UserPackages.SingleOrDefaultAsync(item => item.PackageNumber == number, cancellationToken);
        if (package is null) return NotFound(new { message = "Package number was not found in the scraped package records." });
        if (await db.SupplierCollectionItems.AnyAsync(item => item.PackageId == package.PackageId, cancellationToken))
            return Conflict(new { message = "This package has already been collected." });
        db.SupplierCollectionItems.Add(new SupplierCollectionItem { CollectionId = collectionId, PackageId = package.PackageId });
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("collections/{collectionId:int}/packages/{packageId:int}")]
    public async Task<IActionResult> RemoveCollectionPackage(int collectionId, int packageId, CancellationToken cancellationToken)
    {
        var collection = await AccessibleCollection(collectionId).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (collection is null) return NotFound();
        if (collection.Status != "Open") return Conflict(new { message = "Completed collections cannot be changed." });
        var item = await db.SupplierCollectionItems.SingleOrDefaultAsync(value => value.CollectionId == collectionId && value.PackageId == packageId, cancellationToken);
        if (item is null) return NotFound();
        db.SupplierCollectionItems.Remove(item); await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpPut("collections/{collectionId:int}/complete")]
    public async Task<IActionResult> CompleteCollection(int collectionId, CancellationToken cancellationToken)
    {
        var collection = await AccessibleCollection(collectionId).Include(item => item.Items).SingleOrDefaultAsync(cancellationToken);
        if (collection is null) return NotFound();
        if (collection.Items.Count == 0) return BadRequest(new { message = "Add at least one received package before completing the collection." });
        collection.Status = "Completed"; collection.CompletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    private IQueryable<SupplierCollection> AccessibleCollection(int collectionId)
    {
        var query = db.SupplierCollections.Where(item => item.Id == collectionId);
        if (User.IsInRole(UserRoles.Bearer))
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var bearerId)) return query.Where(_ => false);
            query = query.Where(item => item.BearerUserId == bearerId);
        }
        return query;
    }
}
