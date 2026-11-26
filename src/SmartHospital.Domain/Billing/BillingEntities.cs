using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Billing;

public class ServiceCatalog : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Khám bệnh"; // Khám bệnh, Thủ thuật, Giường, Khác
    public Guid? DepartmentId { get; set; }
    public decimal DefaultPrice { get; set; }
}

public class ServicePriceList : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<ServicePrice> Prices { get; set; } = [];
}

public class ServicePrice : AuditableEntity
{
    public Guid ServicePriceListId { get; set; }
    public ServicePriceList? ServicePriceList { get; set; }
    public string ItemType { get; set; } = "Service"; // Service, LabTest, Imaging, Bed, Medicine
    public Guid ItemId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
}

public class Invoice : AuditableEntity
{
    public string InvoiceCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid? EncounterId { get; set; }
    public Guid? AdmissionId { get; set; }
    public Guid FacilityId { get; set; }
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public InvoiceStatus Status { get; set; } = InvoiceStatus.Issued;
    public decimal TotalAmount { get; set; }
    public decimal InsuranceCoveredAmount { get; set; }
    public decimal PatientResponsibilityAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }
    public string? EInvoiceReference { get; set; }
    public string? Notes { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = [];
    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
}

public class InvoiceItem : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public string SourceType { get; set; } = string.Empty; // Consultation, ServiceOrder, LabOrder, ImagingOrder, PrescriptionDispensing, Bed
    public Guid SourceId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPriceSnapshot { get; set; }
    public decimal LineTotal { get; set; }
    public decimal PayerAmount { get; set; }
    public decimal PatientAmount { get; set; }
}

public class Payment : AuditableEntity
{
    public string ReceiptNumber { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public decimal Amount { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Completed;
    public bool IsAdvance { get; set; }
    public DateTimeOffset PaidAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string CollectedBy { get; set; } = string.Empty;
    public string? ExternalReference { get; set; }
    public string? Notes { get; set; }

    public ICollection<PaymentAllocation> Allocations { get; set; } = [];
    public ICollection<Refund> Refunds { get; set; } = [];
}

public class PaymentAllocation : AuditableEntity
{
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }
    public decimal AllocatedAmount { get; set; }
}

public class Refund : AuditableEntity
{
    public string RefundCode { get; set; } = string.Empty;
    public Guid PaymentId { get; set; }
    public Payment? Payment { get; set; }
    public Guid? InvoiceId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Completed;
    public string RequestedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset RefundedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class BillingAdjustment : AuditableEntity
{
    public Guid InvoiceId { get; set; }
    public decimal AdjustmentAmount { get; set; } // Negative for discount/waiver, positive for surcharge
    public string Reason { get; set; } = string.Empty;
    public string ApprovedBy { get; set; } = string.Empty;
    public DateTimeOffset AdjustedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class InsuranceProfile : AuditableEntity
{
    public Guid PatientId { get; set; }
    public string CardNumber { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidTo { get; set; }
    public string? RegisteredFacilityCode { get; set; }
    public string? Notes { get; set; }
}

public class InsuranceClaim : AuditableEntity
{
    public string ClaimCode { get; set; } = string.Empty;
    public Guid InvoiceId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? InsuranceProfileId { get; set; }
    public decimal ClaimedAmount { get; set; }
    public string Status { get; set; } = "Staging"; // Staging / SubmittedSandbox / Cancelled
    public string? IntegrationMessageRef { get; set; }
    public string? Notes { get; set; }
}
