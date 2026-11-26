using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Contracts.Patients;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Patients;

namespace SmartHospital.Application.Patients;

public record CreateOrUpdatePatientRequest(
    string FullName,
    DateOnly? DateOfBirth,
    Gender Gender,
    string? PhoneNumber,
    string? Email,
    string? NationalId,
    string? Address,
    string? BloodType,
    bool RequiresVerification,
    string? EmergencyContactName,
    string? EmergencyContactRelationship,
    string? EmergencyContactPhone,
    bool ConfirmDuplicateOverride = false);

public interface IPatientService
{
    Task<PagedResult<PatientSummaryDto>> SearchPatientsAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<Patient?> GetPatientDetailAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientDuplicateCandidateDto>> FindDuplicateCandidatesAsync(
        string fullName,
        DateOnly? dateOfBirth,
        string? phoneNumber,
        string? nationalId,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreatePatientAsync(CreateOrUpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdatePatientAsync(Guid id, CreateOrUpdatePatientRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> AddAllergyAsync(Guid patientId, string allergen, AllergySeverity severity, string? reaction, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAllergyAsync(Guid allergyId, CancellationToken cancellationToken = default);
    Task<OperationResult> AddAlertAsync(Guid patientId, string title, string? detail, string severity, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteAlertAsync(Guid alertId, CancellationToken cancellationToken = default);
    Task<OperationResult> AddEmergencyContactAsync(Guid patientId, string fullName, string relationship, string phoneNumber, string? address, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteEmergencyContactAsync(Guid contactId, CancellationToken cancellationToken = default);
    Task<OperationResult> AddInsuranceProfileAsync(Guid patientId, string cardNumber, string? providerName, DateOnly? validFrom, DateOnly? validTo, string? registeredFacilityCode, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteInsuranceProfileAsync(Guid insuranceProfileId, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdatePortalContactAsync(Guid patientId, string? phoneNumber, string? email, string? address, CancellationToken cancellationToken = default);
    Task<OperationResult> MergePatientsAsync(Guid sourcePatientId, Guid targetPatientId, string reason, CancellationToken cancellationToken = default);
}

public class PatientService(
    IHospitalDbContext db,
    ICodeGeneratorService codeGenerator,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IPatientService
{
    public async Task<PagedResult<PatientSummaryDto>> SearchPatientsAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = db.Patients.AsNoTracking().Where(p => !p.IsMerged && p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            var upperTerm = term.ToUpperInvariant();
            query = query.Where(p =>
                p.PatientCode.Contains(upperTerm) ||
                p.NormalizedName.Contains(upperTerm) ||
                p.FullName.Contains(term) ||
                (p.PhoneNumber != null && p.PhoneNumber.Contains(term)) ||
                (p.NationalId != null && p.NationalId.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new PatientSummaryDto(
                p.Id,
                p.PatientCode,
                p.FullName,
                p.DateOfBirth,
                p.Gender.ToString(),
                p.PhoneNumber,
                p.NationalId,
                p.RequiresVerification,
                p.IsDemoData,
                p.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<PatientSummaryDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<Patient?> GetPatientDetailAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await db.Patients.AsNoTracking()
            .Include(p => p.Identifiers)
            .Include(p => p.Contacts)
            .Include(p => p.EmergencyContacts)
            .Include(p => p.Allergies)
            .Include(p => p.Alerts)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PatientDuplicateCandidateDto>> FindDuplicateCandidatesAsync(
        string fullName,
        DateOnly? dateOfBirth,
        string? phoneNumber,
        string? nationalId,
        Guid? excludePatientId = null,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeName(fullName);
        var cleanPhone = phoneNumber?.Trim();
        var cleanNid = nationalId?.Trim();

        var query = db.Patients.AsNoTracking().Where(p => !p.IsMerged && p.IsActive);
        if (excludePatientId.HasValue)
        {
            query = query.Where(p => p.Id != excludePatientId.Value);
        }

        var candidates = await query.Where(p =>
            (!string.IsNullOrEmpty(cleanNid) && p.NationalId == cleanNid) ||
            (p.NormalizedName == normalized && dateOfBirth.HasValue && p.DateOfBirth == dateOfBirth) ||
            (!string.IsNullOrEmpty(cleanPhone) && p.PhoneNumber == cleanPhone && p.NormalizedName == normalized))
            .Take(10)
            .ToListAsync(cancellationToken);

        return candidates.Select(p =>
        {
            var reasons = new List<string>();
            if (!string.IsNullOrEmpty(cleanNid) && p.NationalId == cleanNid) reasons.Add("Trùng số CCCD/CMND");
            if (p.NormalizedName == normalized && dateOfBirth.HasValue && p.DateOfBirth == dateOfBirth) reasons.Add("Trùng họ tên và ngày sinh");
            if (!string.IsNullOrEmpty(cleanPhone) && p.PhoneNumber == cleanPhone && p.NormalizedName == normalized) reasons.Add("Trùng họ tên và SĐT");
            return new PatientDuplicateCandidateDto(
                p.Id, p.PatientCode, p.FullName, p.DateOfBirth, p.PhoneNumber, p.NationalId,
                string.Join(", ", reasons));
        }).ToList();
    }

    public async Task<OperationResult<Guid>> CreatePatientAsync(CreateOrUpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<Guid>.Fail("Họ và tên bệnh nhân là bắt buộc.");

        if (!request.ConfirmDuplicateOverride)
        {
            var duplicates = await FindDuplicateCandidatesAsync(
                request.FullName, request.DateOfBirth, request.PhoneNumber, request.NationalId, null, cancellationToken);

            if (duplicates.Count > 0)
            {
                var first = duplicates[0];
                return OperationResult<Guid>.Fail(
                    $"Cảnh báo trùng lặp hồ sơ: {first.FullName} ({first.PatientCode}) - {first.MatchReason}. Vui lòng kiểm tra lại hoặc xác nhận tạo mới.");
            }
        }

        var code = await codeGenerator.GenerateNextCodeAsync("BN", "Patient", cancellationToken);
        var patient = new Patient
        {
            PatientCode = code,
            FullName = request.FullName.Trim(),
            NormalizedName = NormalizeName(request.FullName),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            PhoneNumber = request.PhoneNumber?.Trim(),
            Email = request.Email?.Trim(),
            NationalId = request.NationalId?.Trim(),
            Address = request.Address?.Trim(),
            BloodType = request.BloodType?.Trim(),
            RequiresVerification = request.RequiresVerification
        };

        if (!string.IsNullOrWhiteSpace(request.NationalId))
        {
            patient.Identifiers.Add(new PatientIdentifier
            {
                PatientId = patient.Id,
                IdentifierType = "CCCD",
                IdentifierValue = request.NationalId.Trim()
            });
        }

        if (!string.IsNullOrWhiteSpace(request.EmergencyContactName) && !string.IsNullOrWhiteSpace(request.EmergencyContactPhone))
        {
            patient.EmergencyContacts.Add(new PatientEmergencyContact
            {
                PatientId = patient.Id,
                FullName = request.EmergencyContactName.Trim(),
                Relationship = string.IsNullOrWhiteSpace(request.EmergencyContactRelationship) ? "Người thân" : request.EmergencyContactRelationship.Trim(),
                PhoneNumber = request.EmergencyContactPhone.Trim()
            });
        }

        db.Patients.Add(patient);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("PatientCreated", "Patient", patient.Id.ToString(),
            afterSummary: new { patient.PatientCode, patient.FullName, patient.DateOfBirth },
            cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(patient.Id, $"Đã tạo hồ sơ bệnh nhân {patient.PatientCode}.");
    }

    public async Task<OperationResult> UpdatePatientAsync(Guid id, CreateOrUpdatePatientRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == id && !p.IsMerged, cancellationToken);
        if (patient is null) return OperationResult.Fail("Không tìm thấy hồ sơ bệnh nhân.");

        var before = new { patient.FullName, patient.DateOfBirth, patient.PhoneNumber, patient.NationalId, patient.Address };

        patient.FullName = request.FullName.Trim();
        patient.NormalizedName = NormalizeName(request.FullName);
        patient.DateOfBirth = request.DateOfBirth;
        patient.Gender = request.Gender;
        patient.PhoneNumber = request.PhoneNumber?.Trim();
        patient.Email = request.Email?.Trim();
        patient.NationalId = request.NationalId?.Trim();
        patient.Address = request.Address?.Trim();
        patient.BloodType = request.BloodType?.Trim();
        patient.RequiresVerification = request.RequiresVerification;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PatientUpdated", "Patient", patient.Id.ToString(),
            beforeSummary: before,
            afterSummary: new { patient.FullName, patient.DateOfBirth, patient.PhoneNumber, patient.NationalId },
            cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã cập nhật thông tin bệnh nhân.");
    }

    public async Task<OperationResult> AddAllergyAsync(Guid patientId, string allergen, AllergySeverity severity, string? reaction, string? notes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(allergen)) return OperationResult.Fail("Tên dị nguyên là bắt buộc.");
        if (!await db.Patients.AnyAsync(p => p.Id == patientId && !p.IsMerged, cancellationToken))
            return OperationResult.Fail("Không tìm thấy bệnh nhân.");

        var item = new PatientAllergy
        {
            PatientId = patientId,
            Allergen = allergen.Trim(),
            Severity = severity,
            Reaction = reaction?.Trim(),
            Notes = notes?.Trim()
        };
        db.PatientAllergies.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PatientAllergyAdded", "Patient", patientId.ToString(),
            afterSummary: new { item.Allergen, Severity = item.Severity.ToString() },
            cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã ghi nhận thông tin dị ứng.");
    }

    public async Task<OperationResult> AddAlertAsync(Guid patientId, string title, string? detail, string severity, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title)) return OperationResult.Fail("Tiêu đề cảnh báo là bắt buộc.");
        if (!await db.Patients.AnyAsync(p => p.Id == patientId && !p.IsMerged, cancellationToken))
            return OperationResult.Fail("Không tìm thấy bệnh nhân.");

        var item = new PatientAlert
        {
            PatientId = patientId,
            AlertTitle = title.Trim(),
            AlertDetail = detail?.Trim(),
            Severity = string.IsNullOrWhiteSpace(severity) ? "Warning" : severity
        };
        db.PatientAlerts.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PatientAlertAdded", "Patient", patientId.ToString(), afterSummary: new { item.AlertTitle, item.Severity }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã thêm cảnh báo quan trọng.");
    }

    public async Task<OperationResult> DeleteAllergyAsync(Guid allergyId, CancellationToken cancellationToken = default)
    {
        var item = await db.PatientAllergies.FirstOrDefaultAsync(x => x.Id == allergyId, cancellationToken);
        if (item is null) return OperationResult.Fail("Không tìm thấy thông tin dị ứng.");
        db.PatientAllergies.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PatientAllergyDeleted", "Patient", item.PatientId.ToString(), afterSummary: new { item.Allergen }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã xóa thông tin dị ứng.");
    }

    public async Task<OperationResult> DeleteAlertAsync(Guid alertId, CancellationToken cancellationToken = default)
    {
        var item = await db.PatientAlerts.FirstOrDefaultAsync(x => x.Id == alertId, cancellationToken);
        if (item is null) return OperationResult.Fail("Không tìm thấy cảnh báo.");
        db.PatientAlerts.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã xóa cảnh báo.");
    }

    public async Task<OperationResult> AddEmergencyContactAsync(Guid patientId, string fullName, string relationship, string phoneNumber, string? address, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phoneNumber))
            return OperationResult.Fail("Họ tên và số điện thoại liên hệ khẩn cấp là bắt buộc.");
        db.PatientEmergencyContacts.Add(new PatientEmergencyContact
        {
            PatientId = patientId,
            FullName = fullName.Trim(),
            Relationship = string.IsNullOrWhiteSpace(relationship) ? "Người thân" : relationship.Trim(),
            PhoneNumber = phoneNumber.Trim(),
            Address = address?.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã thêm liên hệ khẩn cấp.");
    }

    public async Task<OperationResult> DeleteEmergencyContactAsync(Guid contactId, CancellationToken cancellationToken = default)
    {
        var item = await db.PatientEmergencyContacts.FirstOrDefaultAsync(x => x.Id == contactId, cancellationToken);
        if (item is null) return OperationResult.Fail("Không tìm thấy liên hệ khẩn cấp.");
        db.PatientEmergencyContacts.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã xóa liên hệ khẩn cấp.");
    }

    public async Task<OperationResult> AddInsuranceProfileAsync(Guid patientId, string cardNumber, string? providerName, DateOnly? validFrom, DateOnly? validTo, string? registeredFacilityCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(cardNumber)) return OperationResult.Fail("Số thẻ bảo hiểm là bắt buộc.");
        db.InsuranceProfiles.Add(new InsuranceProfile
        {
            PatientId = patientId,
            CardNumber = cardNumber.Trim().ToUpperInvariant(),
            ProviderName = string.IsNullOrWhiteSpace(providerName) ? "BHYT" : providerName.Trim(),
            ValidFrom = validFrom,
            ValidTo = validTo,
            RegisteredFacilityCode = registeredFacilityCode?.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã thêm thẻ bảo hiểm.");
    }

    public async Task<OperationResult> DeleteInsuranceProfileAsync(Guid insuranceProfileId, CancellationToken cancellationToken = default)
    {
        var item = await db.InsuranceProfiles.FirstOrDefaultAsync(x => x.Id == insuranceProfileId, cancellationToken);
        if (item is null) return OperationResult.Fail("Không tìm thấy thẻ bảo hiểm.");
        db.InsuranceProfiles.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã xóa thẻ bảo hiểm.");
    }

    public async Task<OperationResult> UpdatePortalContactAsync(Guid patientId, string? phoneNumber, string? email, string? address, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.FirstOrDefaultAsync(p => p.Id == patientId && !p.IsMerged, cancellationToken);
        if (patient is null) return OperationResult.Fail("Không tìm thấy hồ sơ bệnh nhân.");
        patient.PhoneNumber = phoneNumber?.Trim();
        patient.Email = email?.Trim();
        patient.Address = address?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("PortalContactUpdated", "Patient", patient.Id.ToString(), cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật thông tin liên hệ.");
    }

    public async Task<OperationResult> MergePatientsAsync(Guid sourcePatientId, Guid targetPatientId, string reason, CancellationToken cancellationToken = default)
    {
        if (sourcePatientId == targetPatientId)
            return OperationResult.Fail("Hồ sơ nguồn và hồ sơ đích không được trùng nhau.");
        if (string.IsNullOrWhiteSpace(reason))
            return OperationResult.Fail("Phải nhập lý do hợp nhất hồ sơ.");

        var source = await db.Patients.FirstOrDefaultAsync(p => p.Id == sourcePatientId && !p.IsMerged, cancellationToken);
        var target = await db.Patients.FirstOrDefaultAsync(p => p.Id == targetPatientId && !p.IsMerged, cancellationToken);

        if (source is null || target is null)
            return OperationResult.Fail("Không tìm thấy hồ sơ nguồn hoặc hồ sơ đích hợp lệ.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        // Re-point transactional history without deleting records
        await db.Appointments.Where(a => a.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.Encounters.Where(e => e.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.ClinicalNotes.Where(n => n.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.VitalSigns.Where(v => v.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.LabOrders.Where(o => o.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.ImagingOrders.Where(o => o.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.Prescriptions.Where(p => p.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.Admissions.Where(a => a.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.Invoices.Where(i => i.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);
        await db.Payments.Where(p => p.PatientId == source.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.PatientId, target.Id), cancellationToken);

        source.IsMerged = true;
        source.IsActive = false;
        source.MergedIntoPatientId = target.Id;

        db.PatientMergeHistories.Add(new PatientMergeHistory
        {
            SourcePatientId = source.Id,
            TargetPatientId = target.Id,
            SourcePatientCode = source.PatientCode,
            TargetPatientCode = target.PatientCode,
            Reason = reason.Trim(),
            MergedBy = currentUserService.UserName,
            MergedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("PatientMerged", "Patient", target.Id.ToString(), reason: reason,
            afterSummary: new { Source = source.PatientCode, Target = target.PatientCode },
            cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã hợp nhất hồ sơ {source.PatientCode} vào {target.PatientCode}.");
    }

    private static string NormalizeName(string name)
        => string.Join(' ', name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
}
