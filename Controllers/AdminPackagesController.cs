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
[Route("api/admin/packages")]
public sealed class AdminPackagesController(
    WarehouseDbContext db,
    InvoicePdfService invoicePdfService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResponse<AdminPackageResponse>>> GetPackages(
        [FromQuery] string? status,
        [FromQuery] int? tenantId,
        [FromQuery] string assignment = "all",
        [FromQuery] string batch = "all",
        [FromQuery] string collection = "all",
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 10, 100);

        var query = db.UserPackages.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(package => package.Status == status);
        if (tenantId is int selectedTenant)
            query = query.Where(package => package.Assignment != null &&
                                           package.Assignment.ClientId == selectedTenant);
        if (assignment.Equals("assigned", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.Assignment != null);
        else if (assignment.Equals("unassigned", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.Assignment == null);
        if (batch.Equals("batched", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.BatchItem != null);
        else if (batch.Equals("unbatched", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.BatchItem == null);
        if (collection.Equals("collected", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.SupplierCollectionItem != null);
        else if (collection.Equals("uncollected", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.SupplierCollectionItem == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package =>
                package.PackageNumber.Contains(term) ||
                (package.TrackingId != null && package.TrackingId.Contains(term)) ||
                (package.FullName != null && package.FullName.Contains(term)));
        }

        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(package => package.Created ?? package.CreatedAt)
            .ThenByDescending(package => package.PackageId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(package => new AdminPackageResponse(
                package.PackageId,
                package.SourceUserId,
                package.PackageNumber,
                package.TrackingId,
                package.FullName,
                package.Description,
                package.Status,
                package.Weight,
                package.InvoiceAmount,
                package.AmountDue,
                package.CustomsCharges,
                package.AdditionalMarkup,
                package.SupplierAmount,
                package.SupplierPaidDate,
                package.SupplierPaymentReference,
                package.Assignment == null ? null : package.Assignment.InvoiceCost,
                package.PaidDate,
                package.Created,
                package.Assignment == null ? null : package.Assignment.ClientId,
                package.Assignment == null ? null : package.Assignment.Client.CompanyName,
                package.Assignment == null ? null : package.Assignment.UserId,
                package.Assignment == null ? null : package.Assignment.User.Username,
                package.Assignment == null ? null :
                    ((package.Assignment.User.FirstName ?? "") + " " +
                     (package.Assignment.User.LastName ?? "")).Trim(),
                package.Assignment == null ? null : package.Assignment.AssignedAt,
                package.BatchItem == null ? null : package.BatchItem.BatchId,
                package.BatchItem == null ? null : package.BatchItem.Batch.BatchNumber,
                package.BatchItem == null ? null : package.BatchItem.Batch.FulfillmentMethod,
                package.BatchItem == null ? null : package.BatchItem.Batch.Status,
                package.SupplierCollectionItem == null ? null : package.SupplierCollectionItem.CollectionId,
                package.SupplierCollectionItem == null ? null : package.SupplierCollectionItem.Collection.CollectionNumber,
                package.SupplierCollectionItem == null ? null : package.SupplierCollectionItem.Collection.Status))
            .ToListAsync(cancellationToken);

        return Ok(new PagedResponse<AdminPackageResponse>(
            items, page, pageSize, totalItems,
            (int)Math.Ceiling(totalItems / (double)pageSize)));
    }

    [HttpGet("filters")]
    public async Task<ActionResult<PackageFilterOptions>> GetFilters(
        CancellationToken cancellationToken)
    {
        var statuses = await db.PackageStatuses.AsNoTracking()
            .Where(status => status.IsActive)
            .OrderBy(status => status.DisplayOrder)
            .ThenBy(status => status.Name)
            .Select(status => status.Name)
            .ToListAsync(cancellationToken);
        var tenants = await db.Clients.AsNoTracking()
            .Where(client => client.IsActive)
            .OrderBy(client => client.CompanyName)
            .Select(client => new TenantOption(client.Id, client.CompanyName, client.BatchHandlingMode, client.DefaultDeliveryFee))
            .ToListAsync(cancellationToken);
        return Ok(new PackageFilterOptions(statuses, tenants));
    }

    [HttpGet("status-counts")]
    public async Task<ActionResult<IReadOnlyList<PackageStatusCount>>> GetStatusCounts(
        [FromQuery] int? tenantId,
        [FromQuery] string assignment = "all",
        [FromQuery] string batch = "all",
        [FromQuery] string collection = "all",
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.UserPackages.AsNoTracking().AsQueryable();
        if (tenantId is int selectedTenant)
            query = query.Where(package => package.Assignment != null &&
                                           package.Assignment.ClientId == selectedTenant);
        if (assignment.Equals("assigned", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.Assignment != null);
        else if (assignment.Equals("unassigned", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.Assignment == null);
        if (batch.Equals("batched", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.BatchItem != null);
        else if (batch.Equals("unbatched", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.BatchItem == null);
        if (collection.Equals("collected", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.SupplierCollectionItem != null);
        else if (collection.Equals("uncollected", StringComparison.OrdinalIgnoreCase))
            query = query.Where(package => package.SupplierCollectionItem == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(package =>
                package.PackageNumber.Contains(term) ||
                (package.TrackingId != null && package.TrackingId.Contains(term)) ||
                (package.FullName != null && package.FullName.Contains(term)));
        }

        var counts = await query
            .Where(package => package.Status != null && package.Status != "")
            .GroupBy(package => package.Status!)
            .Select(group => new { Name = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Name, item => item.Count, cancellationToken);

        var statuses = await db.PackageStatuses.AsNoTracking()
            .Where(status => status.IsActive)
            .OrderBy(status => status.DisplayOrder)
            .ThenBy(status => status.Name)
            .Select(status => new { status.Id, status.Name })
            .ToListAsync(cancellationToken);

        return Ok(statuses.Select(status => new PackageStatusCount(
            status.Id,
            status.Name,
            counts.GetValueOrDefault(status.Name))).ToList());
    }

    [HttpGet("tenants/{tenantId:int}/users")]
    public async Task<ActionResult<IReadOnlyList<AuthenticatedUser>>> GetTenantUsers(
        int tenantId, CancellationToken cancellationToken)
    {
        var users = await db.Users.AsNoTracking()
            .Where(user => user.ClientId == tenantId && user.IsActive &&
                           user.Role != UserRoles.SuperAdmin)
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new AuthenticatedUser(
                user.Id, user.ClientId, user.Username, user.FirstName,
                user.LastName, user.Email, user.Role))
            .ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{packageId:int}/invoice")]
    public async Task<IActionResult> DownloadInvoice(
        int packageId,
        CancellationToken cancellationToken)
    {
        var package = await db.UserPackages.AsNoTracking()
            .Include(item => item.Assignment)!.ThenInclude(assignment => assignment!.Client)
            .Include(item => item.Assignment)!.ThenInclude(assignment => assignment!.User)
            .SingleOrDefaultAsync(item => item.PackageId == packageId, cancellationToken);
        if (package is null)
            return NotFound(new { message = "Package does not exist." });
        if (package.Assignment is null)
            return BadRequest(new { message = "Assign the package to a client before downloading an invoice." });

        var pdf = await invoicePdfService.GenerateAsync(
            package,
            package.Assignment,
            package.Assignment.Client,
            cancellationToken);
        var safeNumber = System.Text.RegularExpressions.Regex.Replace(
            package.PackageNumber, "[^A-Za-z0-9_-]+", "-").Trim('-');
        return File(pdf, "application/pdf", $"invoice-{safeNumber}.pdf");
    }

    [HttpPut("{packageId:int}/assignment")]
    public async Task<IActionResult> Assign(
        int packageId,
        AssignPackageRequest request,
        CancellationToken cancellationToken)
    {
        var package = await db.UserPackages.SingleOrDefaultAsync(
            item => item.PackageId == packageId, cancellationToken);
        if (package is null)
            return NotFound(new { message = "Package does not exist." });

        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == request.UserId && candidate.IsActive,
            cancellationToken);
        if (user is null || user.ClientId != request.TenantId || user.Role == UserRoles.SuperAdmin)
            return BadRequest(new { message = "The selected user does not belong to this tenant." });
        var client = await db.Clients.SingleAsync(
            item => item.Id == request.TenantId, cancellationToken);

        var actorClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(actorClaim, out var actorId))
            return Unauthorized();

        var assignment = await db.UserPackageAssignments.SingleOrDefaultAsync(
            item => item.PackageId == packageId, cancellationToken);
        if (assignment is null)
        {
            assignment = new UserPackageAssignment
            {
                PackageId = packageId,
                ClientId = request.TenantId,
                UserId = request.UserId,
                AssignedByUserId = actorId
            };
            db.UserPackageAssignments.Add(assignment);
        }
        else
        {
            assignment.ClientId = request.TenantId;
            assignment.UserId = request.UserId;
            assignment.AssignedByUserId = actorId;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        ApplyInvoiceCost(assignment, package, client);

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("bulk")]
    public async Task<IActionResult> BulkUpdate(
        BulkUpdatePackagesRequest request,
        CancellationToken cancellationToken)
    {
        var packageIds = request.PackageIds.Distinct().ToArray();
        if (packageIds.Length == 0)
            return BadRequest(new { message = "Select at least one package." });
        if (packageIds.Length > 500)
            return BadRequest(new { message = "A maximum of 500 packages can be updated at once." });

        var changesAssignment = request.TenantId.HasValue || request.UserId.HasValue;
        if (changesAssignment && (!request.TenantId.HasValue || !request.UserId.HasValue))
            return BadRequest(new { message = "Select both a client and a user for assignment." });

        var status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim();
        if (!changesAssignment && status is null && !request.UpdatePaidDate)
            return BadRequest(new { message = "Choose an assignment, status, or paid-date update." });
        if (status?.Length > 50)
            return BadRequest(new { message = "Status cannot exceed 50 characters." });
        if (status is not null && !await db.PackageStatuses.AnyAsync(
                item => item.IsActive && item.Name == status, cancellationToken))
            return BadRequest(new { message = "Select an active package status." });

        AppUser? selectedUser = null;
        Client? selectedClient = null;
        if (changesAssignment)
        {
            selectedUser = await db.Users.SingleOrDefaultAsync(user =>
                user.Id == request.UserId && user.IsActive, cancellationToken);
            if (selectedUser is null || selectedUser.ClientId != request.TenantId ||
                selectedUser.Role == UserRoles.SuperAdmin)
                return BadRequest(new { message = "The selected user does not belong to this client." });
            selectedClient = await db.Clients.SingleAsync(
                client => client.Id == request.TenantId, cancellationToken);
        }

        var packages = await db.UserPackages
            .Where(package => packageIds.Contains(package.PackageId))
            .ToListAsync(cancellationToken);
        if (packages.Count != packageIds.Length)
            return BadRequest(new { message = "One or more selected packages no longer exist." });

        if (status is not null)
            foreach (var package in packages)
                package.Status = status;

        var delivered = status?.Equals("Delivered", StringComparison.OrdinalIgnoreCase) == true;
        if (request.UpdatePaidDate || delivered)
            foreach (var package in packages)
            {
                package.PaidDate = request.PaidDate?.Date ??
                    (delivered ? DateTime.UtcNow.Date : null);
                if (package.PaidDate is null)
                    package.AmountDue = package.InvoiceAmount;
            }

        if (selectedUser is not null)
        {
            var actorClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(actorClaim, out var actorId))
                return Unauthorized();

            var assignments = await db.UserPackageAssignments
                .Where(item => packageIds.Contains(item.PackageId))
                .ToDictionaryAsync(item => item.PackageId, cancellationToken);

            foreach (var packageId in packageIds)
            {
                if (!assignments.TryGetValue(packageId, out var assignment))
                {
                    assignment = new UserPackageAssignment { PackageId = packageId };
                    db.UserPackageAssignments.Add(assignment);
                }
                assignment.ClientId = request.TenantId!.Value;
                assignment.UserId = request.UserId!.Value;
                assignment.AssignedByUserId = actorId;
                assignment.UpdatedAt = DateTime.UtcNow;
                ApplyInvoiceCost(
                    assignment,
                    packages.Single(package => package.PackageId == packageId),
                    selectedClient!);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { updated = packages.Count });
    }

    [HttpPut("{packageId:int}")]
    public async Task<IActionResult> UpdatePackage(
        int packageId,
        UpdatePackageRequest request,
        CancellationToken cancellationToken)
    {
        var package = await db.UserPackages.Include(item => item.Assignment).SingleOrDefaultAsync(
            item => item.PackageId == packageId, cancellationToken);
        if (package is null)
            return NotFound(new { message = "Package does not exist." });

        var packageNumber = request.PackageNumber.Trim();
        if (string.IsNullOrWhiteSpace(packageNumber))
            return BadRequest(new { message = "Package number is required." });
        if (packageNumber.Length > 50)
            return BadRequest(new { message = "Package number cannot exceed 50 characters." });
        if (request.TrackingId?.Trim().Length > 100 || request.FullName?.Trim().Length > 100 ||
            request.Description?.Trim().Length > 255 || request.Status?.Trim().Length > 50)
            return BadRequest(new { message = "One or more text values exceed the allowed length." });
        if (new[] { request.Weight, request.AmountDue, request.CustomsCharges, request.AdditionalMarkup, request.SupplierAmount }
            .Any(value => value < 0))
            return BadRequest(new { message = "Package amounts and weight cannot be negative." });

        var status = Optional(request.Status);
        if (status is not null && !await db.PackageStatuses.AnyAsync(
                item => item.IsActive && item.Name == status, cancellationToken))
            return BadRequest(new { message = "Select an active package status." });

        static string? Optional(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        package.PackageNumber = packageNumber;
        package.TrackingId = Optional(request.TrackingId);
        package.FullName = Optional(request.FullName);
        package.Description = Optional(request.Description);
        package.Status = status;
        package.Weight = request.Weight;
        package.AmountDue = request.AmountDue;
        package.CustomsCharges = request.CustomsCharges;
        package.AdditionalMarkup = request.AdditionalMarkup;
        package.SupplierAmount = request.SupplierAmount;
        package.SupplierPaidDate = request.SupplierPaidDate?.Date;
        package.SupplierPaymentReference = string.IsNullOrWhiteSpace(request.SupplierPaymentReference) ? null : request.SupplierPaymentReference.Trim();
        package.PaidDate = request.PaidDate?.Date;
        if (status?.Equals("Delivered", StringComparison.OrdinalIgnoreCase) == true &&
            package.PaidDate is null)
            package.PaidDate = DateTime.UtcNow.Date;
        if (package.PaidDate is null)
            package.AmountDue = package.InvoiceAmount;

        if (package.Assignment is not null)
        {
            var client = await db.Clients.SingleAsync(
                item => item.Id == package.Assignment.ClientId, cancellationToken);
            ApplyInvoiceCost(package.Assignment, package, client);
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{packageId:int}/assignment")]
    public async Task<IActionResult> Unassign(
        int packageId, CancellationToken cancellationToken)
    {
        var assignment = await db.UserPackageAssignments.SingleOrDefaultAsync(
            item => item.PackageId == packageId, cancellationToken);
        if (assignment is null)
            return NoContent();
        db.UserPackageAssignments.Remove(assignment);
        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static void ApplyInvoiceCost(
        UserPackageAssignment assignment,
        UserPackage package,
        Client client)
    {
        assignment.PerLbCost = client.PerLbCost;
        assignment.PerLbMarkup = client.PerLbMarkup;
        assignment.InvoiceCost = decimal.Round(
            (package.Weight ?? 0) * (client.PerLbCost + client.PerLbMarkup) +
            (package.CustomsCharges ?? 0) +
            (package.AdditionalMarkup ?? 0), 2);
        package.InvoiceAmount = assignment.InvoiceCost;
        if (package.PaidDate is null)
            package.AmountDue = package.InvoiceAmount;
    }
}
