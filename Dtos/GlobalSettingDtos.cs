namespace WarehouseApi.Dtos;

public sealed record GlobalSettingResponse(
    int Id,
    string AppName,
    string? LogoUrl,
    string Supplier,
    DateTime UpdatedAt);

public sealed record UpdateGlobalSettingRequest(string AppName, string? LogoUrl, string Supplier);
