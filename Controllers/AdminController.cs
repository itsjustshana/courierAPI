using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.SuperAdmin)]
[Route("apicour/admin")]
public sealed class AdminController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<AdminOverviewResponse>> Overview(
        CancellationToken cancellationToken)
    {
        var roleCounts = await db.Users.AsNoTracking()
            .GroupBy(user => user.Role)
            .Select(group => new { Role = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Role, item => item.Count, cancellationToken);

        return Ok(new AdminOverviewResponse(
            await db.Clients.CountAsync(cancellationToken),
            await db.Clients.CountAsync(client => client.IsActive, cancellationToken),
            await db.Users.CountAsync(cancellationToken),
            await db.Users.CountAsync(user => user.IsActive, cancellationToken),
            await db.UserPackages.CountAsync(package => package.Assignment != null, cancellationToken),
            await db.UserPackages.CountAsync(package => package.Assignment == null, cancellationToken),
            roleCounts));
    }

    [HttpGet("tenants")]
    public async Task<ActionResult<IReadOnlyList<TenantAdminResponse>>> Tenants(
        CancellationToken cancellationToken)
    {
        var tenants = await db.Clients.AsNoTracking()
            .OrderBy(client => client.CompanyName)
            .Select(client => new TenantAdminResponse(
                client.Id,
                client.CompanyName,
                client.ContactName,
                client.Email,
                client.Phone,
                client.Address1,
                client.Address2,
                client.City,
                client.Zip,
                client.State,
                client.LogoUrl,
                client.PerLbCost,
                client.PerLbMarkup,
                client.PerLbCost,
                client.BatchHandlingMode,
                client.DefaultDeliveryFee,
                client.IsActive,
                client.CreatedAt,
                client.Users.Count))
            .ToListAsync(cancellationToken);

        return Ok(tenants);
    }

    [HttpPost("tenants")]
    public async Task<ActionResult<TenantAdminResponse>> CreateTenant(
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateTenantRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var batchMode = ResolveBatchMode(request.BatchHandlingMode)!;
        var tenant = new Client
        {
            CompanyName = request.CompanyName.Trim(),
            ContactName = Clean(request.ContactName, 150),
            Email = Clean(request.Email, 255),
            Phone = Clean(request.Phone, 50),
            Address1 = Clean(request.Address1, 255),
            Address2 = Clean(request.Address2, 255),
            City = Clean(request.City, 100),
            Zip = Clean(request.Zip, 20),
            State = Clean(request.State, 100),
            LogoUrl = Clean(request.LogoUrl, 2048),
            PerLbCost = decimal.Round(request.PerLbCost, 2),
            PerLbMarkup = decimal.Round(request.PerLbMarkup, 2),
            BatchHandlingMode = batchMode,
            DefaultDeliveryFee = decimal.Round(request.DefaultDeliveryFee, 2),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };
        db.Clients.Add(tenant);
        await db.SaveChangesAsync(cancellationToken);
        return Created($"/apicour/admin/tenants/{tenant.Id}", new TenantAdminResponse(
            tenant.Id, tenant.CompanyName, tenant.ContactName, tenant.Email, tenant.Phone,
            tenant.Address1, tenant.Address2, tenant.City, tenant.Zip, tenant.State,
            tenant.LogoUrl, tenant.PerLbCost, tenant.PerLbMarkup, tenant.PerLbRate,
            tenant.BatchHandlingMode, tenant.DefaultDeliveryFee, tenant.IsActive, tenant.CreatedAt, 0));
    }

    [HttpPut("tenants/{tenantId:int}/delivery-fee")]
    public async Task<IActionResult> UpdateTenantDeliveryFee(
        int tenantId,
        UpdateTenantDeliveryFeeRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DefaultDeliveryFee < 0 || request.DefaultDeliveryFee > 99_999_999.99m)
            return BadRequest(new { message = "Default delivery fee must be within the allowed range." });
        var tenant = await db.Clients.SingleOrDefaultAsync(client => client.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new { message = "Tenant does not exist." });
        tenant.DefaultDeliveryFee = decimal.Round(request.DefaultDeliveryFee, 2);
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { tenant.Id, tenant.DefaultDeliveryFee });
    }

    [HttpPut("tenants/{tenantId:int}/batch-mode")]
    public async Task<IActionResult> UpdateTenantBatchMode(
        int tenantId,
        UpdateTenantBatchModeRequest request,
        CancellationToken cancellationToken)
    {
        var mode = request.BatchHandlingMode.Trim();
        var allowed = new[] { "None", "Delivery", "Pickup", "Both" };
        var resolvedMode = allowed.FirstOrDefault(item =>
            item.Equals(mode, StringComparison.OrdinalIgnoreCase));
        if (resolvedMode is null)
            return BadRequest(new { message = "Batch mode must be None, Delivery, Pickup, or Both." });

        var tenant = await db.Clients.SingleOrDefaultAsync(
            client => client.Id == tenantId, cancellationToken);
        if (tenant is null)
            return NotFound(new { message = "Tenant does not exist." });

        tenant.BatchHandlingMode = resolvedMode;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new { tenant.Id, tenant.BatchHandlingMode });
    }

    [HttpPut("tenants/{tenantId:int}")]
    public async Task<IActionResult> UpdateTenant(
        int tenantId,
        UpdateTenantRequest request,
        CancellationToken cancellationToken)
    {
        var validation = ValidateTenantRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var companyName = request.CompanyName.Trim();
        var batchMode = ResolveBatchMode(request.BatchHandlingMode)!;
        var logoUrl = Clean(request.LogoUrl, 2048);
        if (logoUrl is not null && (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && !logoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))))
            return BadRequest(new { message = "Logo URL must be an HTTP(S) or data-image URL." });

        var tenant = await db.Clients.SingleOrDefaultAsync(client => client.Id == tenantId, cancellationToken);
        if (tenant is null) return NotFound(new { message = "Tenant does not exist." });
        tenant.CompanyName = companyName;
        tenant.ContactName = Clean(request.ContactName, 150);
        tenant.Email = Clean(request.Email, 255);
        tenant.Phone = Clean(request.Phone, 50);
        tenant.Address1 = Clean(request.Address1, 255);
        tenant.Address2 = Clean(request.Address2, 255);
        tenant.City = Clean(request.City, 100);
        tenant.Zip = Clean(request.Zip, 20);
        tenant.State = Clean(request.State, 100);
        tenant.LogoUrl = logoUrl;
        tenant.PerLbCost = decimal.Round(request.PerLbCost, 2);
        tenant.PerLbMarkup = decimal.Round(request.PerLbMarkup, 2);
        tenant.BatchHandlingMode = batchMode;
        tenant.DefaultDeliveryFee = decimal.Round(request.DefaultDeliveryFee, 2);
        tenant.IsActive = request.IsActive;

        var assignments = await db.UserPackageAssignments
            .Include(assignment => assignment.Package)
            .Where(assignment => assignment.ClientId == tenantId)
            .ToListAsync(cancellationToken);
        foreach (var assignment in assignments)
        {
            assignment.PerLbCost = tenant.PerLbCost;
            assignment.PerLbMarkup = tenant.PerLbMarkup;
            assignment.InvoiceCost = decimal.Round(
                (assignment.Package.Weight ?? 0) * tenant.PerLbCost + tenant.PerLbMarkup, 2);
            assignment.Package.InvoiceAmount = assignment.InvoiceCost;
            assignment.Package.AmountDue = assignment.Package.PaidDate is null
                ? assignment.InvoiceCost + (assignment.Package.CustomsCharges ?? 0)
                : 0;
            assignment.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPut("tenants/{tenantId:int}/rates")]
    public async Task<IActionResult> UpdateTenantRates(
        int tenantId,
        UpdateTenantRatesRequest request,
        CancellationToken cancellationToken)
    {
        if (request.PerLbCost < 0 || request.PerLbMarkup < 0)
            return BadRequest(new { message = "Per-pound cost and markup cannot be negative." });
        if (request.PerLbCost > 99_999_999.99m || request.PerLbMarkup > 99_999_999.99m)
            return BadRequest(new { message = "Per-pound cost or markup exceeds the allowed amount." });

        var tenant = await db.Clients.SingleOrDefaultAsync(
            client => client.Id == tenantId, cancellationToken);
        if (tenant is null)
            return NotFound(new { message = "Tenant does not exist." });

        tenant.PerLbCost = decimal.Round(request.PerLbCost, 2);
        tenant.PerLbMarkup = decimal.Round(request.PerLbMarkup, 2);

        var assignments = await db.UserPackageAssignments
            .Include(assignment => assignment.Package)
            .Where(assignment => assignment.ClientId == tenantId)
            .ToListAsync(cancellationToken);
        foreach (var assignment in assignments)
        {
            assignment.PerLbCost = tenant.PerLbCost;
            assignment.PerLbMarkup = tenant.PerLbMarkup;
            assignment.InvoiceCost = decimal.Round(
                (assignment.Package.Weight ?? 0) * tenant.PerLbCost + tenant.PerLbMarkup, 2);
            assignment.Package.InvoiceAmount = assignment.InvoiceCost;
            assignment.Package.AmountDue = assignment.Package.PaidDate is null
                ? assignment.InvoiceCost + (assignment.Package.CustomsCharges ?? 0)
                : 0;
            assignment.UpdatedAt = DateTime.UtcNow;
        }
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new {
            tenant.Id,
            tenant.PerLbCost,
            tenant.PerLbMarkup,
            PerLbRate = tenant.PerLbCost
        });
    }

    private static string? Clean(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string? ResolveBatchMode(string? value) =>
        new[] { "None", "Delivery", "Pickup", "Both" }
            .FirstOrDefault(mode => mode.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));

    private static string? ValidateTenantRequest(UpdateTenantRequest request)
    {
        var companyName = request.CompanyName?.Trim();
        if (string.IsNullOrWhiteSpace(companyName) || companyName.Length > 150)
            return "Company name is required and cannot exceed 150 characters.";
        if (request.PerLbCost < 0 || request.PerLbMarkup < 0 || request.DefaultDeliveryFee < 0)
            return "Rates and delivery fees cannot be negative.";
        if (request.PerLbCost > 99_999_999.99m || request.PerLbMarkup > 99_999_999.99m || request.DefaultDeliveryFee > 99_999_999.99m)
            return "A monetary value exceeds the allowed amount.";
        if (ResolveBatchMode(request.BatchHandlingMode) is null)
            return "Batch mode must be None, Delivery, Pickup, or Both.";
        var logoUrl = Clean(request.LogoUrl, 2048);
        if (logoUrl is not null && (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && !logoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))))
            return "Logo URL must be an HTTP(S) or data-image URL.";
        return null;
    }

}
