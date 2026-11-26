using SmartHospital.Contracts.Common;

namespace SmartHospital.Application.Common.Interfaces;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string UserName { get; }
    string FullName { get; }
    bool IsAuthenticated { get; }
    Guid? ActiveFacilityId { get; }
    Guid? ActiveDepartmentId { get; }
    string? CorrelationId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool HasPermission(string permissionCode);
    bool IsInRole(string roleName);
}

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityType,
        string entityId,
        string? reason = null,
        object? beforeSummary = null,
        object? afterSummary = null,
        Guid? facilityId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default);
}

public interface IScopeAuthorizationService
{
    Task<bool> HasHospitalWideScopeAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAllowedFacilityIdsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetAllowedDepartmentIdsAsync(Guid userId, Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<bool> CanAccessFacilityAsync(Guid userId, Guid facilityId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessDepartmentAsync(Guid userId, Guid facilityId, Guid departmentId, CancellationToken cancellationToken = default);
    Task<bool> CanAccessWarehouseAsync(Guid userId, Guid warehouseId, CancellationToken cancellationToken = default);
}

public interface ICodeGeneratorService
{
    Task<string> GenerateNextCodeAsync(string prefix, string sequenceKey, CancellationToken cancellationToken = default);
}

public interface IDocumentStorage
{
    Task<StoredFileResult> SaveAsync(
        Stream content,
        string originalFileName,
        string contentType,
        long sizeBytes,
        CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}

public record StoredFileResult(string StorageKey, string Sha256Checksum, long SizeBytes);

public interface ILabDeviceIntegration
{
    string ProviderName { get; }
    Task<OperationResult<string>> SendOrderToAnalyzerDemoAsync(Guid labOrderId, string orderCode, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IRadiologyIntegration
{
    string ProviderName { get; }
    Task<OperationResult<string>> SendModalityWorklistDemoAsync(Guid imagingOrderId, string orderCode, string modality, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IPacsIntegration
{
    string ProviderName { get; }
    Task<OperationResult<string>> GetDemoViewerUrlAsync(string orderCode, string modality, CancellationToken cancellationToken = default);
}

public interface IInsuranceIntegration
{
    string ProviderName { get; }
    Task<OperationResult<string>> SubmitClaimStagingDemoAsync(Guid claimId, string claimCode, decimal claimedAmount, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IEInvoiceIntegration
{
    string ProviderName { get; }
    Task<OperationResult<string>> IssueDraftEInvoiceDemoAsync(Guid invoiceId, string invoiceCode, decimal totalAmount, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface IPaymentGateway
{
    string ProviderName { get; }
    Task<OperationResult<string>> RecordSandboxTransactionAsync(string receiptNumber, decimal amount, string method, string idempotencyKey, CancellationToken cancellationToken = default);
}

public interface INotificationProvider
{
    string ProviderName { get; }
    Task<OperationResult> SendNotificationAsync(
        Guid? recipientUserId,
        Guid? patientId,
        string channel,
        string title,
        string message,
        string? linkUrl = null,
        CancellationToken cancellationToken = default);
}
