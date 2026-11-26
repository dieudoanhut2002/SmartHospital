using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Reception;

public class Appointment : AuditableEntity
{
    public string AppointmentCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid? DoctorId { get; set; }
    public DateTimeOffset ScheduledAtUtc { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;
    public string? ChiefComplaint { get; set; }
    public string? Notes { get; set; }
    public string? CancelReason { get; set; }
    public Guid? EncounterId { get; set; }
}

public class Encounter : AuditableEntity
{
    public string EncounterCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid? DoctorId { get; set; }
    public Guid? AppointmentId { get; set; }
    public EncounterType EncounterType { get; set; } = EncounterType.Outpatient;
    public EncounterStatus Status { get; set; } = EncounterStatus.CheckedIn;
    public DateTimeOffset StartedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? ChiefComplaint { get; set; }
    public bool IsEmergency { get; set; }
    public string? CancelReason { get; set; }

    public ICollection<QueueTicket> QueueTickets { get; set; } = [];
    public ICollection<EncounterStatusHistory> StatusHistories { get; set; } = [];
}

public class QueueTicket : AuditableEntity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public Guid FacilityId { get; set; }
    public Guid ClinicId { get; set; }
    public DateOnly QueueDate { get; set; }
    public int SequenceNumber { get; set; }
    public string TicketNumber { get; set; } = string.Empty; // e.g. K01-001
    public int Priority { get; set; } // 0 = normal, 1 = ưu tiên
    public QueueTicketStatus Status { get; set; } = QueueTicketStatus.Waiting;
    public DateTimeOffset IssuedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CalledAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class EncounterStatusHistory : BaseEntity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }
    public EncounterStatus FromStatus { get; set; }
    public EncounterStatus ToStatus { get; set; }
    public string? Reason { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public DateTimeOffset ChangedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public class AdmissionRequest : AuditableEntity
{
    public string RequestCode { get; set; } = string.Empty;
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid TargetDepartmentId { get; set; }
    public Guid RequestedByDoctorId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? PreliminaryDiagnosis { get; set; }
    public AdmissionRequestStatus Status { get; set; } = AdmissionRequestStatus.Requested;
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
