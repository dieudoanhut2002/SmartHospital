using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Audit;

public class AuditLog : LogEntity
{
    public Guid? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public Guid? FacilityId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? CorrelationId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Reason { get; set; }
    public string? BeforeSummaryJson { get; set; }
    public string? AfterSummaryJson { get; set; }
}

public class Document : AuditableEntity
{
    public string StorageKey { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256Checksum { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string? Description { get; set; }
}

public class Notification : AuditableEntity
{
    public Guid? RecipientUserId { get; set; }
    public Guid? PatientId { get; set; }
    public string Channel { get; set; } = "InApp";
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTimeOffset? ReadAtUtc { get; set; }
    public string? LinkUrl { get; set; }
}

public class IntegrationMessage : LogEntity
{
    public string ProviderName { get; set; } = string.Empty;
    public string IntegrationType { get; set; } = string.Empty;
    public IntegrationDirection Direction { get; set; }
    public IntegrationStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string? CorrelationId { get; set; }
    public string? Endpoint { get; set; }
    public string? RequestSummary { get; set; }
    public string? ResponseSummary { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; } = 1;
}

public class SystemSetting : AuditableEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
    public string? Description { get; set; }
    public bool IsSecret { get; set; }
}
