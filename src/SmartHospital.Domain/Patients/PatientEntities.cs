using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Patients;

public class Patient : AuditableEntity
{
    public string PatientCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string NormalizedName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; } = Gender.Unknown;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? NationalId { get; set; }
    public string? Address { get; set; }
    public string? BloodType { get; set; }
    public bool RequiresVerification { get; set; }
    public bool IsMerged { get; set; }
    public Guid? MergedIntoPatientId { get; set; }
    public Guid? PortalUserId { get; set; }
    public bool IsDemoData { get; set; }

    public ICollection<PatientIdentifier> Identifiers { get; set; } = [];
    public ICollection<PatientContact> Contacts { get; set; } = [];
    public ICollection<PatientEmergencyContact> EmergencyContacts { get; set; } = [];
    public ICollection<PatientAllergy> Allergies { get; set; } = [];
    public ICollection<PatientAlert> Alerts { get; set; } = [];
}

public class PatientIdentifier : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string IdentifierType { get; set; } = string.Empty; // CCCD, BHYT, Passport, BirthCert
    public string IdentifierValue { get; set; } = string.Empty;
    public DateOnly? IssuedDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public string? IssuedBy { get; set; }
}

public class PatientContact : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string ContactType { get; set; } = "Phone";
    public string Value { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public class PatientEmergencyContact : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Relationship { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? Address { get; set; }
}

public class PatientAllergy : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string Allergen { get; set; } = string.Empty;
    public string? Reaction { get; set; }
    public AllergySeverity Severity { get; set; } = AllergySeverity.Moderate;
    public string? Notes { get; set; }
}

public class PatientAlert : AuditableEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }
    public string AlertTitle { get; set; } = string.Empty;
    public string? AlertDetail { get; set; }
    public string Severity { get; set; } = "Warning";
}

public class PatientMergeHistory : BaseEntity
{
    public Guid SourcePatientId { get; set; }
    public Guid TargetPatientId { get; set; }
    public string SourcePatientCode { get; set; } = string.Empty;
    public string TargetPatientCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string MergedBy { get; set; } = string.Empty;
    public DateTimeOffset MergedAtUtc { get; set; } = DateTimeOffset.UtcNow;
}
