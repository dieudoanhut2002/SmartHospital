using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Clinical;

public class ClinicalNote : AuditableEntity
{
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? ChiefComplaint { get; set; }
    public string? HistoryOfPresentIllness { get; set; }
    public string? PastMedicalHistory { get; set; }
    public string? PhysicalExam { get; set; }
    public string? PreliminaryDiagnosis { get; set; }
    public string? FinalDiagnosis { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? PatientInstructions { get; set; }
    public DateOnly? FollowUpDate { get; set; }
    public ClinicalNoteStatus Status { get; set; } = ClinicalNoteStatus.Draft;
    public int CurrentVersion { get; set; } = 1;
    public DateTimeOffset? SignedAtUtc { get; set; }
    public string? SignedBy { get; set; }

    public ICollection<ClinicalNoteVersion> Versions { get; set; } = [];
}

public class ClinicalNoteVersion : BaseEntity
{
    public Guid ClinicalNoteId { get; set; }
    public ClinicalNote? ClinicalNote { get; set; }
    public int VersionNumber { get; set; }
    public string SnapshotJson { get; set; } = string.Empty;
    public string ChangeType { get; set; } = "Signed"; // Signed / Amendment
    public string? AmendmentReason { get; set; }
    public string RecordedBy { get; set; } = string.Empty;
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class VitalSign : AuditableEntity
{
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string RecordedBy { get; set; } = string.Empty;
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? RespiratoryRate { get; set; }
    public int? SpO2 { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public string? Notes { get; set; }
}

public class Diagnosis : AuditableEntity
{
    public string IcdCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Chapter { get; set; }
}

public class EncounterDiagnosis : AuditableEntity
{
    public Guid EncounterId { get; set; }
    public Guid? DiagnosisId { get; set; }
    public string IcdCode { get; set; } = string.Empty;
    public string DiagnosisName { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public string DiagnosisType { get; set; } = "Confirmed"; // Preliminary / Confirmed
    public string? Notes { get; set; }
}

public class ClinicalOrder : AuditableEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid OrderedByUserId { get; set; }
    public string OrderCategory { get; set; } = "Service"; // Service, Lab, Imaging
    public OrderStatus Status { get; set; } = OrderStatus.Ordered;
    public DateTimeOffset OrderedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? Notes { get; set; }
}

public class ServiceOrder : AuditableEntity
{
    public Guid ClinicalOrderId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid ServiceCatalogId { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPriceSnapshot { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Ordered;
    public string? Notes { get; set; }
    public bool IsBilled { get; set; }
}

public class Prescription : AuditableEntity
{
    public string PrescriptionCode { get; set; } = string.Empty;
    public Guid EncounterId { get; set; }
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid DoctorUserId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Submitted;
    public DateTimeOffset PrescribedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? DiagnosisSummary { get; set; }
    public string? Notes { get; set; }
    public bool IsInpatient { get; set; }

    public ICollection<PrescriptionItem> Items { get; set; } = [];
}

public class PrescriptionItem : AuditableEntity
{
    public Guid PrescriptionId { get; set; }
    public Prescription? Prescription { get; set; }
    public Guid MedicineId { get; set; }
    public string MedicineCode { get; set; } = string.Empty;
    public string MedicineName { get; set; } = string.Empty;
    public string UnitName { get; set; } = string.Empty;
    public string? DosageInstruction { get; set; }
    public string? Route { get; set; }
    public int DurationDays { get; set; } = 1;
    public decimal QuantityPrescribed { get; set; }
    public decimal QuantityDispensed { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
}

public class ClinicalAttachment : AuditableEntity
{
    public Guid EncounterId { get; set; }
    public Guid DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Category { get; set; }
}
