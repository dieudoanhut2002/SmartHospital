using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Patients;
using SmartHospital.Domain.Reception;

namespace SmartHospital.Application.Clinical;

public record EncounterWorklistItemDto(
    Guid EncounterId,
    string EncounterCode,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    DateOnly? DateOfBirth,
    string? ClinicName,
    string DepartmentName,
    EncounterType EncounterType,
    EncounterStatus Status,
    DateTimeOffset StartedAtUtc,
    string? ChiefComplaint);

public record EncounterWorkspaceDto(
    Encounter Encounter,
    Patient Patient,
    string DepartmentName,
    string? ClinicName,
    IReadOnlyList<VitalSign> VitalSigns,
    ClinicalNote? ClinicalNote,
    IReadOnlyList<ClinicalNoteVersion> NoteVersions,
    IReadOnlyList<EncounterDiagnosis> Diagnoses,
    IReadOnlyList<ServiceOrder> ServiceOrders,
    IReadOnlyList<LabOrder> LabOrders,
    IReadOnlyList<ImagingOrder> ImagingOrders,
    IReadOnlyList<Prescription> Prescriptions);

public record RecordVitalSignRequest(
    Guid EncounterId,
    int? SystolicBp,
    int? DiastolicBp,
    int? HeartRate,
    decimal? TemperatureC,
    int? RespiratoryRate,
    int? SpO2,
    decimal? HeightCm,
    decimal? WeightKg,
    string? Notes);

public record SaveClinicalNoteRequest(
    Guid EncounterId,
    string? ChiefComplaint,
    string? HistoryOfPresentIllness,
    string? PastMedicalHistory,
    string? PhysicalExam,
    string? PreliminaryDiagnosis,
    string? FinalDiagnosis,
    string? TreatmentPlan,
    string? PatientInstructions,
    DateOnly? FollowUpDate);

public record CreatePrescriptionItemInput(
    Guid MedicineId,
    string DosageInstruction,
    string? Route,
    int DurationDays,
    decimal QuantityPrescribed);

public interface IClinicalService
{
    Task<PagedResult<EncounterWorklistItemDto>> GetWorklistAsync(PagedRequest request, Guid? facilityId = null, Guid? clinicId = null, EncounterStatus? status = null, CancellationToken cancellationToken = default);
    Task<EncounterWorkspaceDto?> GetEncounterWorkspaceAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<OperationResult> RecordVitalSignAsync(RecordVitalSignRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> SaveClinicalNoteDraftAsync(SaveClinicalNoteRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> SignClinicalNoteAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<OperationResult> AmendClinicalNoteAsync(SaveClinicalNoteRequest request, string amendmentReason, CancellationToken cancellationToken = default);
    Task<OperationResult> AddDiagnosisAsync(Guid encounterId, string icdCode, string diagnosisName, bool isPrimary, string diagnosisType, CancellationToken cancellationToken = default);
    Task<OperationResult> CreateServiceOrderAsync(Guid encounterId, Guid serviceCatalogId, int quantity, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateLabOrderAsync(Guid encounterId, IReadOnlyList<Guid> labTestIds, string? clinicalIndication, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateImagingOrderAsync(Guid encounterId, Guid imagingServiceId, string? clinicalIndication, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreatePrescriptionAsync(Guid encounterId, IReadOnlyList<CreatePrescriptionItemInput> items, string? diagnosisSummary, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> CompleteEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> RequestAdmissionAsync(Guid encounterId, Guid targetDepartmentId, string reason, string? preliminaryDiagnosis, CancellationToken cancellationToken = default);
}

public class ClinicalService(
    IHospitalDbContext db,
    ICurrentUserService currentUserService,
    IScopeAuthorizationService scopeAuth,
    ICodeGeneratorService codeGenerator,
    IAuditService auditService,
    ILabDeviceIntegration labDeviceIntegration,
    IRadiologyIntegration radiologyIntegration) : IClinicalService
{
    public async Task<PagedResult<EncounterWorklistItemDto>> GetWorklistAsync(
        PagedRequest request,
        Guid? facilityId = null,
        Guid? clinicId = null,
        EncounterStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserService.UserId ?? Guid.Empty;
        var hasHospitalScope = await scopeAuth.HasHospitalWideScopeAsync(userId, cancellationToken);
        var allowedFacIds = await scopeAuth.GetAllowedFacilityIdsAsync(userId, cancellationToken);
        var allowedDeptIds = await scopeAuth.GetAllowedDepartmentIdsAsync(userId, facilityId, cancellationToken);

        var query = from e in db.Encounters.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on e.PatientId equals p.Id
                    join d in db.Departments.AsNoTracking() on e.DepartmentId equals d.Id
                    join c in db.Clinics.AsNoTracking() on e.ClinicId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    select new { e, p, d, c };

        if (!hasHospitalScope && allowedFacIds.Count > 0)
        {
            query = query.Where(x => allowedFacIds.Contains(x.e.FacilityId));
        }
        if (!hasHospitalScope && allowedDeptIds.Count > 0 && currentUserService.IsInRole(RoleNames.Doctor))
        {
            query = query.Where(x => allowedDeptIds.Contains(x.e.DepartmentId) || x.d.DepartmentType == DepartmentType.Outpatient);
        }

        if (facilityId.HasValue) query = query.Where(x => x.e.FacilityId == facilityId.Value);
        if (clinicId.HasValue) query = query.Where(x => x.e.ClinicId == clinicId.Value);
        if (status.HasValue) query = query.Where(x => x.e.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.e.EncounterCode.Contains(term) ||
                x.p.PatientCode.Contains(term) ||
                x.p.FullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.e.StartedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new EncounterWorklistItemDto(
                x.e.Id, x.e.EncounterCode, x.p.Id, x.p.PatientCode, x.p.FullName, x.p.DateOfBirth,
                x.c != null ? x.c.Name : null, x.d.Name, x.e.EncounterType, x.e.Status,
                x.e.StartedAtUtc, x.e.ChiefComplaint))
            .ToListAsync(cancellationToken);

        return new PagedResult<EncounterWorklistItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<EncounterWorkspaceDto?> GetEncounterWorkspaceAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return null;

        var userId = currentUserService.UserId ?? Guid.Empty;
        if (!await scopeAuth.CanAccessFacilityAsync(userId, encounter.FacilityId, cancellationToken))
        {
            throw new DomainException("Bạn không có quyền truy cập lượt khám thuộc cơ sở này.");
        }

        var patient = await db.Patients.AsNoTracking()
            .Include(p => p.Allergies)
            .Include(p => p.Alerts)
            .FirstAsync(p => p.Id == encounter.PatientId, cancellationToken);

        var dept = await db.Departments.AsNoTracking().FirstAsync(d => d.Id == encounter.DepartmentId, cancellationToken);
        var clinic = encounter.ClinicId.HasValue
            ? await db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == encounter.ClinicId.Value, cancellationToken)
            : null;

        var vitals = await db.VitalSigns.AsNoTracking()
            .Where(v => v.EncounterId == encounterId)
            .OrderByDescending(v => v.RecordedAtUtc)
            .ToListAsync(cancellationToken);

        var note = await db.ClinicalNotes.AsNoTracking()
            .FirstOrDefaultAsync(n => n.EncounterId == encounterId, cancellationToken);

        var noteVersions = note is null
            ? []
            : await db.ClinicalNoteVersions.AsNoTracking()
                .Where(v => v.ClinicalNoteId == note.Id)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync(cancellationToken);

        var diagnoses = await db.EncounterDiagnoses.AsNoTracking()
            .Where(d => d.EncounterId == encounterId)
            .ToListAsync(cancellationToken);

        var serviceOrders = await db.ServiceOrders.AsNoTracking()
            .Where(s => s.EncounterId == encounterId)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var labOrders = await db.LabOrders.AsNoTracking()
            .Include(l => l.Items)
            .ThenInclude(i => i.Result)
            .Include(l => l.Specimens)
            .Where(l => l.EncounterId == encounterId)
            .OrderByDescending(l => l.OrderedAtUtc)
            .ToListAsync(cancellationToken);

        var imagingOrders = await db.ImagingOrders.AsNoTracking()
            .Include(i => i.Report)
            .Where(i => i.EncounterId == encounterId)
            .OrderByDescending(i => i.OrderedAtUtc)
            .ToListAsync(cancellationToken);

        var prescriptions = await db.Prescriptions.AsNoTracking()
            .Include(p => p.Items)
            .Where(p => p.EncounterId == encounterId)
            .OrderByDescending(p => p.PrescribedAtUtc)
            .ToListAsync(cancellationToken);

        return new EncounterWorkspaceDto(
            encounter, patient, dept.Name, clinic?.Name,
            vitals, note, noteVersions, diagnoses, serviceOrders, labOrders, imagingOrders, prescriptions);
    }

    public async Task<OperationResult> RecordVitalSignAsync(RecordVitalSignRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == request.EncounterId, cancellationToken);
        if (encounter is null) return OperationResult.Fail("Không tìm thấy lượt khám.");

        db.VitalSigns.Add(new VitalSign
        {
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            RecordedAtUtc = DateTimeOffset.UtcNow,
            RecordedBy = currentUserService.FullName,
            SystolicBp = request.SystolicBp,
            DiastolicBp = request.DiastolicBp,
            HeartRate = request.HeartRate,
            TemperatureC = request.TemperatureC,
            RespiratoryRate = request.RespiratoryRate,
            SpO2 = request.SpO2,
            HeightCm = request.HeightCm,
            WeightKg = request.WeightKg,
            Notes = request.Notes?.Trim()
        });

        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã ghi nhận chỉ số sinh hiệu.");
    }

    public async Task<OperationResult<Guid>> SaveClinicalNoteDraftAsync(SaveClinicalNoteRequest request, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == request.EncounterId, cancellationToken);
        if (encounter is null) return OperationResult<Guid>.Fail("Không tìm thấy lượt khám.");

        var note = await db.ClinicalNotes.FirstOrDefaultAsync(n => n.EncounterId == encounter.Id, cancellationToken);
        if (note is not null && note.Status != ClinicalNoteStatus.Draft)
        {
            return OperationResult<Guid>.Fail("Bệnh án đã được ký xác nhận, không thể sửa trực tiếp. Vui lòng dùng chức năng Bổ sung/Đính chính (Amendment) và ghi lý do.");
        }

        if (note is null)
        {
            note = new ClinicalNote
            {
                EncounterId = encounter.Id,
                PatientId = encounter.PatientId,
                FacilityId = encounter.FacilityId,
                DepartmentId = encounter.DepartmentId,
                AuthorUserId = currentUserService.UserId ?? Guid.Empty,
                AuthorName = currentUserService.FullName,
                Status = ClinicalNoteStatus.Draft,
                CurrentVersion = 1
            };
            db.ClinicalNotes.Add(note);
        }

        ApplyNoteContent(note, request);
        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("ClinicalNoteDraftSaved", "ClinicalNote", note.Id.ToString(),
            facilityId: encounter.FacilityId, departmentId: encounter.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(note.Id, "Đã lưu nháp phiếu khám.");
    }

    public async Task<OperationResult> SignClinicalNoteAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var note = await db.ClinicalNotes.FirstOrDefaultAsync(n => n.EncounterId == encounterId, cancellationToken);
        if (note is null) return OperationResult.Fail("Chưa có phiếu khám để ký.");
        if (note.Status != ClinicalNoteStatus.Draft)
            return OperationResult.Fail("Phiếu khám đã được ký trước đó.");

        note.Status = ClinicalNoteStatus.Signed;
        note.SignedAtUtc = DateTimeOffset.UtcNow;
        note.SignedBy = currentUserService.FullName;

        db.ClinicalNoteVersions.Add(new ClinicalNoteVersion
        {
            ClinicalNoteId = note.Id,
            VersionNumber = note.CurrentVersion,
            SnapshotJson = SerializeNoteSnapshot(note),
            ChangeType = "Signed",
            RecordedBy = currentUserService.FullName,
            RecordedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ClinicalNoteSigned", "ClinicalNote", note.Id.ToString(),
            afterSummary: new { note.CurrentVersion, note.FinalDiagnosis, note.SignedBy },
            facilityId: note.FacilityId, departmentId: note.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã ký và khóa phiên bản phiếu khám.");
    }

    public async Task<OperationResult> AmendClinicalNoteAsync(SaveClinicalNoteRequest request, string amendmentReason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(amendmentReason))
            return OperationResult.Fail("Phải nhập lý do khi bổ sung/đính chính bệnh án đã ký.");

        var note = await db.ClinicalNotes.FirstOrDefaultAsync(n => n.EncounterId == request.EncounterId, cancellationToken);
        if (note is null) return OperationResult.Fail("Không tìm thấy phiếu khám.");
        if (note.Status == ClinicalNoteStatus.Draft)
            return OperationResult.Fail("Phiếu khám đang là bản nháp, hãy lưu nháp bình thường.");

        var beforeSnapshot = new { note.CurrentVersion, note.FinalDiagnosis, note.TreatmentPlan };

        ApplyNoteContent(note, request);
        note.CurrentVersion += 1;
        note.Status = ClinicalNoteStatus.Amended;
        note.SignedAtUtc = DateTimeOffset.UtcNow;
        note.SignedBy = currentUserService.FullName;

        db.ClinicalNoteVersions.Add(new ClinicalNoteVersion
        {
            ClinicalNoteId = note.Id,
            VersionNumber = note.CurrentVersion,
            SnapshotJson = SerializeNoteSnapshot(note),
            ChangeType = "Amendment",
            AmendmentReason = amendmentReason.Trim(),
            RecordedBy = currentUserService.FullName,
            RecordedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ClinicalNoteAmended", "ClinicalNote", note.Id.ToString(), reason: amendmentReason,
            beforeSummary: beforeSnapshot,
            afterSummary: new { note.CurrentVersion, note.FinalDiagnosis, note.TreatmentPlan },
            facilityId: note.FacilityId, departmentId: note.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã lưu bản đính chính phiên bản {note.CurrentVersion}.");
    }

    public async Task<OperationResult> AddDiagnosisAsync(Guid encounterId, string icdCode, string diagnosisName, bool isPrimary, string diagnosisType, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult.Fail("Không tìm thấy lượt khám.");
        if (string.IsNullOrWhiteSpace(diagnosisName)) return OperationResult.Fail("Tên chẩn đoán là bắt buộc.");

        var catalog = await db.Diagnoses.AsNoTracking().FirstOrDefaultAsync(d => d.IcdCode == icdCode, cancellationToken);

        db.EncounterDiagnoses.Add(new EncounterDiagnosis
        {
            EncounterId = encounterId,
            DiagnosisId = catalog?.Id,
            IcdCode = icdCode.Trim().ToUpperInvariant(),
            DiagnosisName = diagnosisName.Trim(),
            IsPrimary = isPrimary,
            DiagnosisType = string.IsNullOrWhiteSpace(diagnosisType) ? "Confirmed" : diagnosisType
        });

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã thêm chẩn đoán.");
    }

    public async Task<OperationResult> CreateServiceOrderAsync(Guid encounterId, Guid serviceCatalogId, int quantity, string? notes, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return OperationResult.Fail("Số lượng phải lớn hơn 0.");
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult.Fail("Không tìm thấy lượt khám.");

        var svc = await db.ServiceCatalogs.AsNoTracking().FirstOrDefaultAsync(s => s.Id == serviceCatalogId && s.IsActive, cancellationToken);
        if (svc is null) return OperationResult.Fail("Không tìm thấy dịch vụ.");

        var price = await ResolveEffectivePriceAsync("Service", svc.Id, svc.DefaultPrice, cancellationToken);
        var clinicalOrder = new ClinicalOrder
        {
            OrderCode = await codeGenerator.GenerateNextCodeAsync("CDDV", "ClinicalOrder", cancellationToken),
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            FacilityId = encounter.FacilityId,
            DepartmentId = encounter.DepartmentId,
            OrderedByUserId = currentUserService.UserId ?? Guid.Empty,
            OrderCategory = "Service",
            Notes = notes?.Trim()
        };
        db.ClinicalOrders.Add(clinicalOrder);

        db.ServiceOrders.Add(new ServiceOrder
        {
            ClinicalOrderId = clinicalOrder.Id,
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            ServiceCatalogId = svc.Id,
            ServiceCode = svc.Code,
            ServiceName = svc.Name,
            Quantity = quantity,
            UnitPriceSnapshot = price,
            Notes = notes?.Trim()
        });

        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Đã chỉ định dịch vụ {svc.Name}.");
    }

    public async Task<OperationResult<Guid>> CreateLabOrderAsync(Guid encounterId, IReadOnlyList<Guid> labTestIds, string? clinicalIndication, CancellationToken cancellationToken = default)
    {
        if (labTestIds.Count == 0) return OperationResult<Guid>.Fail("Vui lòng chọn ít nhất một xét nghiệm.");
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult<Guid>.Fail("Không tìm thấy lượt khám.");

        var tests = await db.LabTests.AsNoTracking().Where(t => labTestIds.Contains(t.Id) && t.IsActive).ToListAsync(cancellationToken);
        if (tests.Count == 0) return OperationResult<Guid>.Fail("Danh mục xét nghiệm không hợp lệ.");

        var order = new LabOrder
        {
            OrderCode = await codeGenerator.GenerateNextCodeAsync("XN", "LabOrder", cancellationToken),
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            FacilityId = encounter.FacilityId,
            DepartmentId = encounter.DepartmentId,
            OrderedByUserId = currentUserService.UserId ?? Guid.Empty,
            OrderedByName = currentUserService.FullName,
            ClinicalIndication = clinicalIndication?.Trim(),
            Status = LabOrderStatus.Ordered
        };

        foreach (var t in tests)
        {
            var price = await ResolveEffectivePriceAsync("LabTest", t.Id, t.DefaultPrice, cancellationToken);
            order.Items.Add(new LabOrderItem
            {
                LabOrderId = order.Id,
                LabTestId = t.Id,
                TestCode = t.Code,
                TestName = t.Name,
                UnitPriceSnapshot = price,
                Status = LabOrderStatus.Ordered
            });
        }

        // Create initial Specimen record awaiting collection
        var barcode = $"SMP-{HospitalTime.ToVietnamTime(DateTimeOffset.UtcNow):yyMMddHHmmss}-{Random.Shared.Next(100, 999)}";
        order.Specimens.Add(new Specimen
        {
            LabOrderId = order.Id,
            Barcode = barcode,
            SpecimenType = tests.First().SpecimenType,
            Status = SpecimenStatus.PendingCollection
        });

        db.LabOrders.Add(order);
        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await labDeviceIntegration.SendOrderToAnalyzerDemoAsync(order.Id, order.OrderCode, order.OrderCode, cancellationToken);
        await auditService.LogAsync("LabOrderCreated", "LabOrder", order.Id.ToString(),
            afterSummary: new { order.OrderCode, TestCount = order.Items.Count },
            facilityId: encounter.FacilityId, departmentId: encounter.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(order.Id, $"Đã tạo chỉ định xét nghiệm {order.OrderCode} (Barcode: {barcode}).");
    }

    public async Task<OperationResult<Guid>> CreateImagingOrderAsync(Guid encounterId, Guid imagingServiceId, string? clinicalIndication, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult<Guid>.Fail("Không tìm thấy lượt khám.");

        var svc = await db.ImagingServices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == imagingServiceId && i.IsActive, cancellationToken);
        if (svc is null) return OperationResult<Guid>.Fail("Không tìm thấy dịch vụ chẩn đoán hình ảnh.");

        var price = await ResolveEffectivePriceAsync("Imaging", svc.Id, svc.DefaultPrice, cancellationToken);
        var order = new ImagingOrder
        {
            OrderCode = await codeGenerator.GenerateNextCodeAsync("CDHA", "ImagingOrder", cancellationToken),
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            FacilityId = encounter.FacilityId,
            ImagingServiceId = svc.Id,
            ServiceCode = svc.Code,
            ServiceName = svc.Name,
            Modality = svc.Modality,
            UnitPriceSnapshot = price,
            OrderedByUserId = currentUserService.UserId ?? Guid.Empty,
            OrderedByName = currentUserService.FullName,
            ClinicalIndication = clinicalIndication?.Trim(),
            Status = ImagingOrderStatus.Ordered
        };

        db.ImagingOrders.Add(order);
        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await radiologyIntegration.SendModalityWorklistDemoAsync(order.Id, order.OrderCode, order.Modality, order.OrderCode, cancellationToken);
        await auditService.LogAsync("ImagingOrderCreated", "ImagingOrder", order.Id.ToString(),
            afterSummary: new { order.OrderCode, order.ServiceName },
            facilityId: encounter.FacilityId, departmentId: encounter.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(order.Id, $"Đã tạo chỉ định CĐHA {order.OrderCode}.");
    }

    public async Task<OperationResult<Guid>> CreatePrescriptionAsync(
        Guid encounterId,
        IReadOnlyList<CreatePrescriptionItemInput> items,
        string? diagnosisSummary,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return OperationResult<Guid>.Fail("Đơn thuốc phải có ít nhất một thuốc.");
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult<Guid>.Fail("Không tìm thấy lượt khám.");

        var medIds = items.Select(i => i.MedicineId).Distinct().ToList();
        var meds = await db.Medicines.AsNoTracking().Where(m => medIds.Contains(m.Id) && m.IsActive).ToDictionaryAsync(m => m.Id, cancellationToken);

        var prescription = new Prescription
        {
            PrescriptionCode = await codeGenerator.GenerateNextCodeAsync("DT", "Prescription", cancellationToken),
            EncounterId = encounter.Id,
            PatientId = encounter.PatientId,
            FacilityId = encounter.FacilityId,
            DepartmentId = encounter.DepartmentId,
            DoctorUserId = currentUserService.UserId ?? Guid.Empty,
            DoctorName = currentUserService.FullName,
            Status = PrescriptionStatus.Submitted,
            PrescribedAtUtc = DateTimeOffset.UtcNow,
            DiagnosisSummary = diagnosisSummary?.Trim(),
            Notes = notes?.Trim(),
            IsInpatient = encounter.EncounterType == EncounterType.Inpatient
        };

        foreach (var item in items)
        {
            if (item.QuantityPrescribed <= 0) return OperationResult<Guid>.Fail("Số lượng thuốc kê phải lớn hơn 0.");
            if (!meds.TryGetValue(item.MedicineId, out var med)) return OperationResult<Guid>.Fail("Danh mục thuốc không hợp lệ.");

            prescription.Items.Add(new PrescriptionItem
            {
                PrescriptionId = prescription.Id,
                MedicineId = med.Id,
                MedicineCode = med.Code,
                MedicineName = med.Name,
                UnitName = med.BaseUnit,
                DosageInstruction = item.DosageInstruction.Trim(),
                Route = string.IsNullOrWhiteSpace(item.Route) ? med.DefaultRoute : item.Route.Trim(),
                DurationDays = Math.Max(1, item.DurationDays),
                QuantityPrescribed = item.QuantityPrescribed,
                UnitPriceSnapshot = med.DefaultUnitPrice
            });
        }

        db.Prescriptions.Add(prescription);
        await EnsureEncounterInProgressAsync(encounter, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("PrescriptionCreated", "Prescription", prescription.Id.ToString(),
            afterSummary: new { prescription.PrescriptionCode, ItemCount = prescription.Items.Count },
            facilityId: encounter.FacilityId, departmentId: encounter.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(prescription.Id, $"Đã kê đơn thuốc {prescription.PrescriptionCode}.");
    }

    public async Task<OperationResult> CompleteEncounterAsync(Guid encounterId, CancellationToken cancellationToken = default)
    {
        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult.Fail("Không tìm thấy lượt khám.");
        if (encounter.Status == EncounterStatus.Completed) return OperationResult.Ok("Lượt khám đã hoàn tất.");
        if (encounter.Status == EncounterStatus.Cancelled) return OperationResult.Fail("Không thể hoàn tất lượt khám đã hủy.");

        var oldStatus = encounter.Status;
        encounter.Status = EncounterStatus.Completed;
        encounter.CompletedAtUtc = DateTimeOffset.UtcNow;

        db.EncounterStatusHistories.Add(new EncounterStatusHistory
        {
            EncounterId = encounter.Id,
            FromStatus = oldStatus,
            ToStatus = EncounterStatus.Completed,
            ChangedBy = currentUserService.UserName,
            Reason = "Hoàn tất lượt khám"
        });

        // Mark active queue tickets as Completed
        var activeTickets = await db.QueueTickets
            .Where(t => t.EncounterId == encounter.Id && (t.Status == QueueTicketStatus.Waiting || t.Status == QueueTicketStatus.Called || t.Status == QueueTicketStatus.InProgress))
            .ToListAsync(cancellationToken);
        foreach (var t in activeTickets)
        {
            t.Status = QueueTicketStatus.Completed;
            t.CompletedAtUtc = DateTimeOffset.UtcNow;
        }

        if (encounter.AppointmentId.HasValue)
        {
            var appt = await db.Appointments.FirstOrDefaultAsync(a => a.Id == encounter.AppointmentId.Value, cancellationToken);
            if (appt is not null) appt.Status = AppointmentStatus.Completed;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EncounterCompleted", "Encounter", encounter.Id.ToString(),
            beforeSummary: new { Status = oldStatus.ToString() },
            afterSummary: new { Status = EncounterStatus.Completed.ToString() },
            facilityId: encounter.FacilityId, departmentId: encounter.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã hoàn tất lượt khám {encounter.EncounterCode}.");
    }

    public async Task<OperationResult<Guid>> RequestAdmissionAsync(Guid encounterId, Guid targetDepartmentId, string reason, string? preliminaryDiagnosis, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult<Guid>.Fail("Vui lòng nhập lý do chỉ định nhập viện.");
        var encounter = await db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encounterId, cancellationToken);
        if (encounter is null) return OperationResult<Guid>.Fail("Không tìm thấy lượt khám.");

        var req = new AdmissionRequest
        {
            RequestCode = await codeGenerator.GenerateNextCodeAsync("NV", "AdmissionRequest", cancellationToken),
            PatientId = encounter.PatientId,
            EncounterId = encounter.Id,
            FacilityId = encounter.FacilityId,
            TargetDepartmentId = targetDepartmentId,
            RequestedByDoctorId = currentUserService.UserId ?? Guid.Empty,
            Reason = reason.Trim(),
            PreliminaryDiagnosis = preliminaryDiagnosis?.Trim(),
            Status = AdmissionRequestStatus.Requested
        };

        db.AdmissionRequests.Add(req);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("AdmissionRequested", "AdmissionRequest", req.Id.ToString(),
            afterSummary: new { req.RequestCode, req.TargetDepartmentId },
            facilityId: encounter.FacilityId, departmentId: targetDepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(req.Id, $"Đã tạo yêu cầu nhập viện {req.RequestCode}.");
    }

    private async Task EnsureEncounterInProgressAsync(Encounter encounter, CancellationToken cancellationToken)
    {
        if (encounter.Status is EncounterStatus.Waiting or EncounterStatus.CheckedIn or EncounterStatus.Booked)
        {
            var old = encounter.Status;
            encounter.Status = EncounterStatus.InProgress;
            db.EncounterStatusHistories.Add(new EncounterStatusHistory
            {
                EncounterId = encounter.Id,
                FromStatus = old,
                ToStatus = EncounterStatus.InProgress,
                ChangedBy = currentUserService.UserName,
                Reason = "Bắt đầu xử lý chuyên môn"
            });
            await Task.CompletedTask;
        }
    }

    private async Task<decimal> ResolveEffectivePriceAsync(string itemType, Guid itemId, decimal fallbackPrice, CancellationToken cancellationToken)
    {
        var today = HospitalTime.TodayVietnam();
        var price = await (
            from sp in db.ServicePrices.AsNoTracking()
            join pl in db.ServicePriceLists.AsNoTracking() on sp.ServicePriceListId equals pl.Id
            where pl.IsActive && pl.EffectiveFrom <= today && (pl.EffectiveTo == null || pl.EffectiveTo >= today)
                  && sp.ItemType == itemType && sp.ItemId == itemId && sp.IsActive
            orderby pl.IsDefault descending, pl.EffectiveFrom descending
            select (decimal?)sp.UnitPrice).FirstOrDefaultAsync(cancellationToken);

        return price ?? fallbackPrice;
    }

    private static void ApplyNoteContent(ClinicalNote note, SaveClinicalNoteRequest request)
    {
        note.ChiefComplaint = request.ChiefComplaint?.Trim();
        note.HistoryOfPresentIllness = request.HistoryOfPresentIllness?.Trim();
        note.PastMedicalHistory = request.PastMedicalHistory?.Trim();
        note.PhysicalExam = request.PhysicalExam?.Trim();
        note.PreliminaryDiagnosis = request.PreliminaryDiagnosis?.Trim();
        note.FinalDiagnosis = request.FinalDiagnosis?.Trim();
        note.TreatmentPlan = request.TreatmentPlan?.Trim();
        note.PatientInstructions = request.PatientInstructions?.Trim();
        note.FollowUpDate = request.FollowUpDate;
    }

    private static string SerializeNoteSnapshot(ClinicalNote note)
    {
        return JsonSerializer.Serialize(new
        {
            note.ChiefComplaint,
            note.HistoryOfPresentIllness,
            note.PastMedicalHistory,
            note.PhysicalExam,
            note.PreliminaryDiagnosis,
            note.FinalDiagnosis,
            note.TreatmentPlan,
            note.PatientInstructions,
            note.FollowUpDate,
            note.AuthorName,
            note.SignedBy,
            note.SignedAtUtc
        });
    }
}
