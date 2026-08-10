namespace WarehouseApi.Dtos;

public sealed record AdminPackageResponse(
    int PackageId,
    int? SourceUserId,
    string PackageNumber,
    string? TrackingId,
    string? FullName,
    string? Description,
    string? Status,
    decimal? Weight,
    decimal? InvoiceAmount,
    decimal? AmountDue,
    decimal? CustomsCharges,
    decimal? AdditionalMarkup,
    decimal? SupplierAmount,
    DateTime? SupplierPaidDate,
    string? SupplierPaymentReference,
    decimal? InvoiceCost,
    DateTime? PaidDate,
    DateTime? Created,
    int? TenantId,
    string? TenantName,
    int? AssignedUserId,
    string? AssignedLogin,
    string? AssignedUserName,
    DateTime? AssignedAt,
    int? BatchId,
    string? BatchNumber,
    string? BatchMethod,
    string? BatchStatus,
    int? CollectionId,
    string? CollectionNumber,
    string? CollectionStatus);

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalItems,
    int TotalPages);

public sealed record AssignPackageRequest(int TenantId, int UserId);

public sealed record BulkUpdatePackagesRequest(
    IReadOnlyList<int> PackageIds,
    int? TenantId,
    int? UserId,
    string? Status,
    bool UpdatePaidDate = false,
    DateTime? PaidDate = null);

public sealed record UpdatePackageRequest(
    string PackageNumber,
    string? TrackingId,
    string? FullName,
    string? Description,
    string? Status,
    decimal? Weight,
    decimal? AmountDue,
    decimal? CustomsCharges,
    decimal? AdditionalMarkup,
    decimal? SupplierAmount,
    DateTime? SupplierPaidDate,
    string? SupplierPaymentReference,
    DateTime? PaidDate);

public sealed record PackageFilterOptions(
    IReadOnlyList<string> Statuses,
    IReadOnlyList<TenantOption> Tenants);

public sealed record TenantOption(int Id, string Name, string BatchHandlingMode, decimal DefaultDeliveryFee);

public sealed record PackageStatusCount(int Id, string Name, int Count);
