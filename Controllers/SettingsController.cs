using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WarehouseApi.Data;
using WarehouseApi.Dtos;

namespace WarehouseApi.Controllers;

[ApiController]
[Route("apicour/settings")]
public sealed class SettingsController(WarehouseDbContext db) : ControllerBase
{
    [HttpGet]
    [HttpGet("public")]
    public async Task<ActionResult<GlobalSettingResponse>> Get(CancellationToken cancellationToken)
    {
        var setting = await db.GlobalSettings.AsNoTracking().OrderBy(item => item.Id).FirstOrDefaultAsync(cancellationToken);
        if (setting is null) return NotFound(new { message = "Application settings have not been configured." });
        return Ok(new GlobalSettingResponse(setting.Id, setting.AppName, setting.LogoUrl, setting.Supplier, setting.UpdatedAt));
    }
}
