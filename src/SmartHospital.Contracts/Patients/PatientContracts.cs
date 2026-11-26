namespace SmartHospital.Contracts.Patients;

public record PatientSummaryDto(
    Guid Id,
    string PatientCode,
    string FullName,
    DateOnly? DateOfBirth,
    string Gender,
    string? PhoneNumber,
    string? NationalId,
    bool RequiresVerification,
    bool IsDemoData,
    DateTimeOffset CreatedAtUtc);

public record PatientDuplicateCandidateDto(
    Guid Id,
    string PatientCode,
    string FullName,
    DateOnly? DateOfBirth,
    string? PhoneNumber,
    string? NationalId,
    string MatchReason);
