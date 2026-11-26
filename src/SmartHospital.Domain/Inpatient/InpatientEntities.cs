using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Inpatient;

public class Admission : AuditableEntity
{
    public string AdmissionCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid? AdmissionRequestId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? AttendingDoctorId { get; set; }
    public DateTimeOffset AdmittedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? DischargedAtUtc { get; set; }
    public AdmissionStatus Status { get; set; } = AdmissionStatus.Active;
    public string? AdmissionDiagnosis { get; set; }
    public string? DischargeSummary { get; set; }
    public string? DischargeInstructions { get; set; }
    public DateOnly? FollowUpDate { get; set; }

    public ICollection<BedAssignment> BedAssignments { get; set; } = [];
}

public class BedAssignment : AuditableEntity
{
    public Guid AdmissionId { get; set; }
    public Admission? Admission { get; set; }
    public Guid BedId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RoomId { get; set; }
    public DateTimeOffset AssignedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReleasedAtUtc { get; set; }
    public bool IsCurrent { get; set; } = true;
    public string? TransferReason { get; set; }
}

public class BedStatusHistory : BaseEntity
{
    public Guid BedId { get; set; }
    public BedStatus FromStatus { get; set; }
    public BedStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class NursingAssignment : AuditableEntity
{
    public Guid AdmissionId { get; set; }
    public Guid NurseUserId { get; set; }
    public string NurseName { get; set; } = string.Empty;
    public DateOnly AssignedDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
}

public class NursingNote : AuditableEntity
{
    public Guid AdmissionId { get; set; }
    public Guid PatientId { get; set; }
    public DateTimeOffset RecordedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string RecordedBy { get; set; } = string.Empty;
    public string NoteText { get; set; } = string.Empty;
}

public class NursingObservation : AuditableEntity
{
    public Guid AdmissionId { get; set; }
    public Guid PatientId { get; set; }
    public DateTimeOffset ObservedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string RecordedBy { get; set; } = string.Empty;
    public int? SystolicBp { get; set; }
    public int? DiastolicBp { get; set; }
    public int? HeartRate { get; set; }
    public decimal? TemperatureC { get; set; }
    public int? SpO2 { get; set; }
    public string? FluidIntakeOutput { get; set; }
    public bool IsOutOfThreshold { get; set; }
    public string? AlertNote { get; set; }
}

public class MedicationAdministration : AuditableEntity
{
    public Guid AdmissionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid? PrescriptionItemId { get; set; }
    public string MedicineName { get; set; } = string.Empty;
    public string Dose { get; set; } = string.Empty;
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public DateTimeOffset? AdministeredAtUtc { get; set; }
    public string Status { get; set; } = "Administered"; // Administered / Omitted
    public string? OmissionReason { get; set; }
    public string RecordedBy { get; set; } = string.Empty;
}

public class ShiftHandoverNote : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public string ShiftName { get; set; } = string.Empty;
    public string HandoverFrom { get; set; } = string.Empty;
    public string HandoverTo { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
}

public class EmergencyIntake : AuditableEntity
{
    public string EmergencyCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid FacilityId { get; set; }
    public DateTimeOffset ArrivedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public EmergencyTriageLevel TriageLevel { get; set; } = EmergencyTriageLevel.Level3Urgent;
    public string TriagedBy { get; set; } = string.Empty;
    public string InitialCondition { get; set; } = string.Empty;
    public string? BayLocation { get; set; }
    public string Status { get; set; } = "Active"; // Active / Admitted / Discharged / Transferred
}

public class SurgeryRequest : AuditableEntity
{
    public string RequestCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid? AdmissionId { get; set; }
    public Guid FacilityId { get; set; }
    public string ProcedureName { get; set; } = string.Empty;
    public Guid RequestedByDoctorId { get; set; }
    public DateTimeOffset? ScheduledAtUtc { get; set; }
    public Guid? OperatingRoomId { get; set; }
    public string? TeamSummary { get; set; }
    public string? ChecklistSummary { get; set; }
    public string? SuppliesUsedSummary { get; set; }
    public string? OperativeNote { get; set; }
    public SurgeryStatus Status { get; set; } = SurgeryStatus.Requested;
}
