namespace WarehouseApi.Dtos;

public sealed record SupplierSettlementResponse(
    int PackageId, string PackageNumber, string? TrackingId, string? CustomerName,
    string? PackageStatus, int? TenantId, string? TenantName,
    decimal? SupplierAmount, DateTime? SupplierPaidDate, string? PaymentReference,
    int? CollectionId, string? CollectionNumber,
    DateTime? Created);

public sealed record SupplierSettlementPage(
    IReadOnlyList<SupplierSettlementResponse> Items,
    int Page, int PageSize, int TotalItems, int TotalPages,
    decimal TotalPayable, decimal OutstandingPayable, decimal PaidPayable);

public sealed record UpdateSupplierSettlementRequest(
    decimal? SupplierAmount, DateTime? PaidDate, string? PaymentReference);

public sealed record CreateSupplierCollectionRequest(string? SupplierName, int? BearerUserId, string? Notes, DateTime? CollectionDate, IReadOnlyList<int>? PackageIds);
public sealed record UpdateCollectionDateRequest(DateTime CollectionDate);
public sealed record UpdateCollectionPaidDateRequest(DateTime? PaidDate);
public sealed record AddCollectionPackageRequest(string PackageNumber);
public sealed record SupplierCollectionResponse(int Id, string CollectionNumber, string SupplierName,
    int? BearerUserId, string? BearerName, string Status, int PackageCount,
    string? Notes, DateTime CollectionDate, DateTime? PaidDate, DateTime CreatedAt, DateTime? CompletedAt);
public sealed record CollectionAvailablePackageResponse(int PackageId, string PackageNumber,
    string? TrackingId, string? CustomerName, string? Description, string? Status, DateTime? Created);
