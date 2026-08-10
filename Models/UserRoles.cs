namespace WarehouseApi.Models;

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantOwner = "TenantOwner";
    public const string Dispatcher = "Dispatcher";
    public const string Driver = "Driver";
    public const string Customer = "Customer";
    public const string Bearer = "Bearer";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        SuperAdmin,
        TenantOwner,
        Dispatcher,
        Driver,
        Customer,
        Bearer
    };
}
