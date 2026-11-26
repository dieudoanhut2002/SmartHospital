using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Laboratory;

public class LabTest : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = "Máu";
    public string? Unit { get; set; }
    public string? ReferenceRangeText { get; set; }
    public decimal? ReferenceMin { get; set; }
    public decimal? ReferenceMax { get; set; }
    public decimal DefaultPrice { get; set; }
}

public class LabOrder : AuditableEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid OrderedByUserId { get; set; }
    public string OrderedByName { get; set; } = string.Empty;
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;
    public DateTimeOffset OrderedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ClinicalIndication { get; set; }
    public Guid? AssignedTechnicianId { get; set; }
    public bool IsBilled { get; set; }

    public ICollection<LabOrderItem> Items { get; set; } = [];
    public ICollection<Specimen> Specimens { get; set; } = [];
}

public class LabOrderItem : AuditableEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }
    public Guid LabTestId { get; set; }
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public LabOrderStatus Status { get; set; } = LabOrderStatus.Ordered;

    public LabResult? Result { get; set; }
}

public class Specimen : AuditableEntity
{
    public Guid LabOrderId { get; set; }
    public LabOrder? LabOrder { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string SpecimenType { get; set; } = string.Empty;
    public string? StorageLocation { get; set; }
    public SpecimenStatus Status { get; set; } = SpecimenStatus.PendingCollection;
    public DateTimeOffset? CollectedAtUtc { get; set; }
    public string? CollectedBy { get; set; }
    public DateTimeOffset? ReceivedAtUtc { get; set; }
    public string? ReceivedBy { get; set; }
    public string? RejectionReason { get; set; }
}

public class LabResult : AuditableEntity
{
    public Guid LabOrderItemId { get; set; }
    public LabOrderItem? LabOrderItem { get; set; }
    public string? ResultValue { get; set; }
    public decimal? NumericValue { get; set; }
    public string? Unit { get; set; }
    public string? ReferenceRange { get; set; }
    public bool IsAbnormal { get; set; }
    public string? Notes { get; set; }
    public bool IsVerified { get; set; }
    public int Version { get; set; } = 1;
    public string? EnteredBy { get; set; }
    public DateTimeOffset? EnteredAtUtc { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public bool IsReleasedToPortal { get; set; } = true;

    public ICollection<LabResultVersion> Versions { get; set; } = [];
}

public class LabResultVersion : BaseEntity
{
    public Guid LabResultId { get; set; }
    public LabResult? LabResult { get; set; }
    public int VersionNumber { get; set; }
    public string? ResultValue { get; set; }
    public decimal? NumericValue { get; set; }
    public bool IsAbnormal { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class ImagingService : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Modality { get; set; } = "X-Quang"; // X-Quang, Siêu âm, CT, MRI
    public decimal DefaultPrice { get; set; }
}

public class ImagingOrder : AuditableEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ImagingServiceId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string Modality { get; set; } = string.Empty;
    public decimal UnitPriceSnapshot { get; set; }
    public Guid OrderedByUserId { get; set; }
    public string OrderedByName { get; set; } = string.Empty;
    public ImagingOrderStatus Status { get; set; } = ImagingOrderStatus.Ordered;
    public DateTimeOffset OrderedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public Guid? TechnicianId { get; set; }
    public string? ClinicalIndication { get; set; }
    public bool IsBilled { get; set; }

    public ImagingReport? Report { get; set; }
}

public class ImagingReport : AuditableEntity
{
    public Guid ImagingOrderId { get; set; }
    public ImagingOrder? ImagingOrder { get; set; }
    public string Findings { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public string? AttachmentStorageKey { get; set; }
    public string? ExternalDemoViewerUrl { get; set; }
    public bool IsVerified { get; set; }
    public string? ReportedBy { get; set; }
    public DateTimeOffset? ReportedAtUtc { get; set; }
    public string? VerifiedBy { get; set; }
    public DateTimeOffset? VerifiedAtUtc { get; set; }
    public bool IsReleasedToPortal { get; set; } = true;
}
