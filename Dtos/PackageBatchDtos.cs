namespace WarehouseApi.Dtos;

public sealed record CreatePackageBatchRequest(
    IReadOnlyList<int> PackageIds,
    string FulfillmentMethod,
    DateTime? ScheduledDate,
    string? Notes,
    decimal? DeliveryFee,
    string? DeliveryArea,
    string? DeliveryAddress,
    string? DeliveryFeeOverrideReason);

public sealed record PackageBatchResponse(
    int Id,
    string BatchNumber,
    int ClientId,
    string ClientName,
    string FulfillmentMethod,
    string Status,
    DateTime? ScheduledDate,
    DateTime? CompletedDate,
    int PackageCount,
    DateTime CreatedAt,
    string? Notes,
    decimal DeliveryFee,
    string? DeliveryArea,
    string? DeliveryAddress,
    string DeliveryFeeSource,
    string? DeliveryFeeOverrideReason,
    decimal InvoiceTotal,
    decimal AmountDue,
    DateTime? PaidDate);

public sealed record UpdatePackageBatchStatusRequest(string Status);
public sealed record UpdateBatchDeliveryFeeRequest(decimal DeliveryFee, string? DeliveryArea, string? DeliveryAddress, string? OverrideReason);
public sealed record UpdateBatchPaidDateRequest(DateTime? PaidDate);

public sealed record BatchPackageResponse(
    int PackageId,
    string PackageNumber,
    string? TrackingId,
    string? CustomerName,
    string? Description,
    string? Status,
    decimal? Weight,
    DateTime AddedAt,
    bool CanRemove);
