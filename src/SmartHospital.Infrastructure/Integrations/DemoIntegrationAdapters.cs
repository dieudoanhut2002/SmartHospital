using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Common;
using SmartHospital.Infrastructure.Persistence;

namespace SmartHospital.Infrastructure.Integrations;

public class DemoIntegrationBase(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger logger)
{
    protected async Task<OperationResult<string>> ExecuteIdempotentDemoAsync(
        string providerName,
        string integrationType,
        string idempotencyKey,
        string endpoint,
        string requestSummary,
        Func<string> simulateReference,
        CancellationToken cancellationToken)
    {
        var existing = await db.IntegrationMessages
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.IntegrationType == integrationType && x.IdempotencyKey == idempotencyKey && x.Status == IntegrationStatus.DemoSimulated,
                cancellationToken);

        if (existing is not null)
        {
            return OperationResult<string>.Ok(
                existing.ResponseSummary ?? existing.IdempotencyKey,
                $"[Demo/Sandbox] Đã xử lý trước đó (idempotent) bởi {providerName}.");
        }

        var externalRef = simulateReference();
        var msg = new IntegrationMessage
        {
            ProviderName = providerName,
            IntegrationType = integrationType,
            Direction = IntegrationDirection.Outbound,
            Status = IntegrationStatus.DemoSimulated,
            IdempotencyKey = idempotencyKey,
            CorrelationId = currentUserService.CorrelationId,
            Endpoint = endpoint,
            RequestSummary = requestSummary,
            ResponseSummary = externalRef,
            AttemptCount = 1,
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.IntegrationMessages.Add(msg);
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Demo integration {IntegrationType} via {ProviderName} executed with key {IdempotencyKey}",
            integrationType, providerName, idempotencyKey);

        return OperationResult<string>.Ok(externalRef, $"[Demo/Sandbox] {providerName} đã ghi nhận yêu cầu mô phỏng.");
    }
}

public class DemoLabDeviceIntegration(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger<DemoLabDeviceIntegration> logger)
    : DemoIntegrationBase(db, currentUserService, logger), ILabDeviceIntegration
{
    public string ProviderName => "Demo-LIS-HL7-Sandbox";

    public Task<OperationResult<string>> SendOrderToAnalyzerDemoAsync(Guid labOrderId, string orderCode, string idempotencyKey, CancellationToken cancellationToken = default)
        => ExecuteIdempotentDemoAsync(
            ProviderName,
            "LIS_ANALYZER_DEMO",
            idempotencyKey,
            "sandbox://lis/hl7/orm-o01",
            $"OrderCode={orderCode};LabOrderId={labOrderId}",
            () => $"DEMO-LIS-{orderCode}",
            cancellationToken);
}

public class DemoRadiologyIntegration(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger<DemoRadiologyIntegration> logger)
    : DemoIntegrationBase(db, currentUserService, logger), IRadiologyIntegration
{
    public string ProviderName => "Demo-RIS-Worklist-Sandbox";

    public Task<OperationResult<string>> SendModalityWorklistDemoAsync(Guid imagingOrderId, string orderCode, string modality, string idempotencyKey, CancellationToken cancellationToken = default)
        => ExecuteIdempotentDemoAsync(
            ProviderName,
            "RIS_MWL_DEMO",
            idempotencyKey,
            "sandbox://ris/dicom-mwl",
            $"OrderCode={orderCode};Modality={modality}",
            () => $"DEMO-MWL-{orderCode}",
            cancellationToken);
}

public class DemoPacsIntegration : IPacsIntegration
{
    public string ProviderName => "Demo-PACS-Viewer-Sandbox";

    public Task<OperationResult<string>> GetDemoViewerUrlAsync(string orderCode, string modality, CancellationToken cancellationToken = default)
    {
        var url = $"/Imaging/Orders/DemoViewer?orderCode={Uri.EscapeDataString(orderCode)}&modality={Uri.EscapeDataString(modality)}";
        return Task.FromResult(OperationResult<string>.Ok(url, "[Demo/Sandbox] Liên kết xem ảnh PACS mô phỏng."));
    }
}

public class DemoInsuranceIntegration(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger<DemoInsuranceIntegration> logger)
    : DemoIntegrationBase(db, currentUserService, logger), IInsuranceIntegration
{
    public string ProviderName => "Demo-BHYT-Staging-Sandbox";

    public Task<OperationResult<string>> SubmitClaimStagingDemoAsync(Guid claimId, string claimCode, decimal claimedAmount, string idempotencyKey, CancellationToken cancellationToken = default)
        => ExecuteIdempotentDemoAsync(
            ProviderName,
            "BHYT_CLAIM_STAGING_DEMO",
            idempotencyKey,
            "sandbox://bhyt/staging",
            $"ClaimCode={claimCode};Amount={claimedAmount:0.##}",
            () => $"STAGING-BHYT-{claimCode}",
            cancellationToken);
}

public class DemoEInvoiceIntegration(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger<DemoEInvoiceIntegration> logger)
    : DemoIntegrationBase(db, currentUserService, logger), IEInvoiceIntegration
{
    public string ProviderName => "Demo-EInvoice-Sandbox";

    public Task<OperationResult<string>> IssueDraftEInvoiceDemoAsync(Guid invoiceId, string invoiceCode, decimal totalAmount, string idempotencyKey, CancellationToken cancellationToken = default)
        => ExecuteIdempotentDemoAsync(
            ProviderName,
            "EINVOICE_DEMO",
            idempotencyKey,
            "sandbox://einvoice/draft",
            $"InvoiceCode={invoiceCode};Total={totalAmount:0.##}",
            () => $"DEMO-HDĐT-{invoiceCode}",
            cancellationToken);
}

public class DemoPaymentGateway(
    HospitalDbContext db,
    ICurrentUserService currentUserService,
    ILogger<DemoPaymentGateway> logger)
    : DemoIntegrationBase(db, currentUserService, logger), IPaymentGateway
{
    public string ProviderName => "Demo-PaymentGateway-Sandbox";

    public Task<OperationResult<string>> RecordSandboxTransactionAsync(string receiptNumber, decimal amount, string method, string idempotencyKey, CancellationToken cancellationToken = default)
        => ExecuteIdempotentDemoAsync(
            ProviderName,
            "PAYMENT_GATEWAY_DEMO",
            idempotencyKey,
            "sandbox://payments/record",
            $"Receipt={receiptNumber};Method={method};Amount={amount:0.##}",
            () => $"DEMO-TXN-{receiptNumber}",
            cancellationToken);
}

public class DemoNotificationProvider(
    HospitalDbContext db,
    ILogger<DemoNotificationProvider> logger) : INotificationProvider
{
    public string ProviderName => "Demo-InApp-Notification";

    public async Task<OperationResult> SendNotificationAsync(
        Guid? recipientUserId,
        Guid? patientId,
        string channel,
        string title,
        string message,
        string? linkUrl = null,
        CancellationToken cancellationToken = default)
    {
        db.Notifications.Add(new Notification
        {
            RecipientUserId = recipientUserId,
            PatientId = patientId,
            Channel = channel,
            Title = title,
            Message = message,
            LinkUrl = linkUrl,
            IsRead = false
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Notification queued via {ProviderName}: {Title}", ProviderName, title);
        return OperationResult.Ok("Đã ghi nhận thông báo nội bộ.");
    }
}
