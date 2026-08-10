using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;
using WarehouseApi.Models;

namespace WarehouseApi.Controllers;

[ApiController]
[Authorize(Roles = UserRoles.SuperAdmin)]
[Route("api/admin/global-settings")]
public sealed class AdminGlobalSettingsController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<GlobalSettingResponse>> Get(CancellationToken cancellationToken)
    {
        var setting = await db.GlobalSettings.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (setting is null) return NotFound(new { message = "Application settings have not been configured." });
        return Ok(new GlobalSettingResponse(setting.Id, setting.AppName, setting.LogoUrl, setting.Supplier, setting.UpdatedAt));
    }

    [HttpPut]
    public async Task<ActionResult<GlobalSettingResponse>> Update(
        UpdateGlobalSettingRequest request,
        CancellationToken cancellationToken)
    {
        var appName = request.AppName?.Trim();
        if (string.IsNullOrWhiteSpace(appName) || appName.Length > 100)
            return BadRequest(new { message = "Application name is required and cannot exceed 100 characters." });
        var logoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        var supplier = request.Supplier?.Trim();
        if (string.IsNullOrWhiteSpace(supplier) || supplier.Length > 150)
            return BadRequest(new { message = "Supplier is required and cannot exceed 150 characters." });
        if (logoUrl is not null && (!Uri.TryCreate(logoUrl, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps && !logoUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))))
            return BadRequest(new { message = "Logo URL must be an HTTP(S) or data-image URL." });

        var setting = await db.GlobalSettings.OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (setting is null)
        {
            setting = new GlobalSetting { CreatedAt = DateTime.UtcNow };
            db.GlobalSettings.Add(setting);
        }
        setting.AppName = appName;
        setting.LogoUrl = logoUrl;
        setting.Supplier = supplier;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Ok(new GlobalSettingResponse(setting.Id, setting.AppName, setting.LogoUrl, setting.Supplier, setting.UpdatedAt));
    }
}
