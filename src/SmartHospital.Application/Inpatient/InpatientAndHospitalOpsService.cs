using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Inpatient;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Reception;

namespace SmartHospital.Application.Inpatient;

public record AdmissionListItemDto(
    Guid AdmissionId,
    string AdmissionCode,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    string DepartmentName,
    string? CurrentBedCode,
    DateTimeOffset AdmittedAtUtc,
    AdmissionStatus Status);

public record BedBoardItemDto(
    Guid BedId,
    string BedCode,
    string BedName,
    string RoomName,
    string DepartmentName,
    BedStatus Status,
    decimal DailyRate,
    Guid? ActiveAdmissionId,
    string? PatientCode,
    string? PatientName);

public record DashboardMetricsDto(
    int EncountersToday,
    int WaitingQueueCount,
    int ActiveAdmissionsCount,
    int TotalBedsCount,
    int OccupiedBedsCount,
    int PendingLabOrdersCount,
    int PendingLabVerifyCount,
    int PendingPrescriptionsCount,
    decimal RevenueToday,
    int LowStockBatchCount);

public interface IInpatientAndHospitalOpsService
{
    // Inpatient & Beds
    Task<IReadOnlyList<BedBoardItemDto>> GetBedBoardAsync(Guid facilityId, Guid? departmentId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionListItemDto>> GetAdmissionsAsync(Guid facilityId, AdmissionStatus? status = null, CancellationToken cancellationToken = default);
    Task<Admission?> GetAdmissionDetailAsync(Guid admissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AdmissionRequest>> GetPendingAdmissionRequestsAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> AdmitPatientAsync(Guid patientId, Guid encounterId, Guid facilityId, Guid departmentId, Guid bedId, string? admissionDiagnosis, Guid? admissionRequestId = null, CancellationToken cancellationToken = default);
    Task<OperationResult> TransferBedAsync(Guid admissionId, Guid newBedId, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult> DischargePatientAsync(Guid admissionId, string dischargeSummary, string? dischargeInstructions, DateOnly? followUpDate, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateBedStatusAsync(Guid bedId, BedStatus newStatus, string? reason, CancellationToken cancellationToken = default);

    // Nursing
    Task<OperationResult> AddNursingNoteAsync(Guid admissionId, string noteText, CancellationToken cancellationToken = default);
    Task<OperationResult> RecordNursingObservationAsync(Guid admissionId, int? sysBp, int? diaBp, int? hr, decimal? temp, int? spo2, string? fluidIo, CancellationToken cancellationToken = default);
    Task<OperationResult> RecordMedicationAdministrationAsync(Guid admissionId, string medicineName, string dose, string status, string? omissionReason, CancellationToken cancellationToken = default);
    Task<OperationResult> AddShiftHandoverAsync(Guid facilityId, Guid departmentId, string shiftName, string handoverTo, string summary, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NursingNote>> GetNursingNotesAsync(Guid admissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<NursingObservation>> GetNursingObservationsAsync(Guid admissionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicationAdministration>> GetMedicationAdministrationsAsync(Guid admissionId, CancellationToken cancellationToken = default);

    // Emergency
    Task<IReadOnlyList<EmergencyIntake>> GetEmergencyIntakesAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateEmergencyIntakeAsync(Guid patientId, Guid facilityId, EmergencyTriageLevel triageLevel, string initialCondition, string? bayLocation, CancellationToken cancellationToken = default);

    // Surgery
    Task<IReadOnlyList<SurgeryRequest>> GetSurgeryRequestsAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateSurgeryRequestAsync(Guid patientId, Guid encounterId, Guid facilityId, string procedureName, DateTimeOffset? scheduledAtUtc, Guid? operatingRoomId, string? teamSummary, string? checklistSummary, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateSurgeryStatusAsync(Guid surgeryId, SurgeryStatus status, string? operativeNote, string? suppliesUsed, CancellationToken cancellationToken = default);

    // HR
    Task<IReadOnlyList<Employee>> GetEmployeesAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateEmployeeAsync(Guid facilityId, Guid? departmentId, string employeeCode, string fullName, string? jobTitle, string? phone, string? email, DateOnly? hireDate, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateEmployeeAsync(Guid employeeId, Guid? departmentId, string fullName, string? jobTitle, string? phone, string? email, bool isActive, bool isTerminated, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShiftSchedule>> GetShiftSchedulesAsync(Guid facilityId, DateOnly? date = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateShiftScheduleAsync(Guid facilityId, Guid departmentId, Guid employeeId, Guid? clinicId, DateOnly shiftDate, string shiftName, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default);
    Task<OperationResult> DeleteShiftScheduleAsync(Guid shiftId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaveRecord>> GetLeaveRecordsAsync(Guid facilityId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateLeaveRecordAsync(Guid employeeId, DateOnly fromDate, DateOnly toDate, string leaveType, string reason, CancellationToken cancellationToken = default);

    // Dashboard
    Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid facilityId, CancellationToken cancellationToken = default);
}

public class InpatientAndHospitalOpsService(
    IHospitalDbContext db,
    ICodeGeneratorService codeGenerator,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IInpatientAndHospitalOpsService
{
    public async Task<IReadOnlyList<BedBoardItemDto>> GetBedBoardAsync(Guid facilityId, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var query = from b in db.Beds.AsNoTracking()
                    join r in db.Rooms.AsNoTracking() on b.RoomId equals r.Id
                    join d in db.Departments.AsNoTracking() on b.DepartmentId equals d.Id
                    where b.FacilityId == facilityId && b.IsActive
                    select new { b, r, d };

        if (departmentId.HasValue) query = query.Where(x => x.b.DepartmentId == departmentId.Value);

        var beds = await query.OrderBy(x => x.b.Code).ToListAsync(cancellationToken);
        var activeAssignments = await (
            from ba in db.BedAssignments.AsNoTracking()
            join adm in db.Admissions.AsNoTracking() on ba.AdmissionId equals adm.Id
            join p in db.Patients.AsNoTracking() on adm.PatientId equals p.Id
            where ba.IsCurrent && adm.FacilityId == facilityId
            select new { ba.BedId, adm.Id, p.PatientCode, p.FullName })
            .ToDictionaryAsync(x => x.BedId, cancellationToken);

        return beds.Select(x =>
        {
            activeAssignments.TryGetValue(x.b.Id, out var occ);
            return new BedBoardItemDto(
                x.b.Id, x.b.Code, x.b.Name, x.r.Name, x.d.Name, x.b.Status, x.b.DailyRate,
                occ?.Id, occ?.PatientCode, occ?.FullName);
        }).ToList();
    }

    public async Task<IReadOnlyList<AdmissionListItemDto>> GetAdmissionsAsync(Guid facilityId, AdmissionStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = from a in db.Admissions.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                    join d in db.Departments.AsNoTracking() on a.DepartmentId equals d.Id
                    where a.FacilityId == facilityId
                    select new { a, p, d };

        if (status.HasValue) query = query.Where(x => x.a.Status == status.Value);

        var rows = await query.OrderByDescending(x => x.a.AdmittedAtUtc).ToListAsync(cancellationToken);
        var admIds = rows.Select(r => r.a.Id).ToList();

        Dictionary<Guid, string> currentBeds = await (
            from ba in db.BedAssignments.AsNoTracking()
            join b in db.Beds.AsNoTracking() on ba.BedId equals b.Id
            where ba.IsCurrent && admIds.Contains(ba.AdmissionId)
            select new { ba.AdmissionId, b.Code })
            .ToDictionaryAsync(x => x.AdmissionId, x => x.Code, cancellationToken);

        return rows.Select(r => new AdmissionListItemDto(
            r.a.Id, r.a.AdmissionCode, r.p.Id, r.p.PatientCode, r.p.FullName, r.d.Name,
            currentBeds.GetValueOrDefault(r.a.Id), r.a.AdmittedAtUtc, r.a.Status)).ToList();
    }

    public async Task<Admission?> GetAdmissionDetailAsync(Guid admissionId, CancellationToken cancellationToken = default)
        => await db.Admissions.AsNoTracking().Include(a => a.BedAssignments).FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);

    public async Task<IReadOnlyList<AdmissionRequest>> GetPendingAdmissionRequestsAsync(Guid facilityId, CancellationToken cancellationToken = default)
        => await db.AdmissionRequests.AsNoTracking()
            .Where(r => r.FacilityId == facilityId && r.Status == AdmissionRequestStatus.Requested)
            .OrderByDescending(r => r.RequestedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<OperationResult<Guid>> AdmitPatientAsync(
        Guid patientId, Guid encounterId, Guid facilityId, Guid departmentId, Guid bedId,
        string? admissionDiagnosis, Guid? admissionRequestId = null, CancellationToken cancellationToken = default)
    {
        Bed? bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == bedId && b.FacilityId == facilityId, cancellationToken);
        if (bed is null) return OperationResult<Guid>.Fail("Không tìm thấy giường bệnh.");
        if (bed.Status != BedStatus.Available)
            return OperationResult<Guid>.Fail($"Giường {bed.Code} hiện ở trạng thái {bed.Status}, không thể tiếp nhận bệnh nhân.");

        var hasActiveAssignment = await db.BedAssignments.AnyAsync(ba => ba.BedId == bedId && ba.IsCurrent, cancellationToken);
        if (hasActiveAssignment)
            return OperationResult<Guid>.Fail($"Giường {bed.Code} đang có bệnh nhân sử dụng.");

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var admission = new Admission
        {
            AdmissionCode = await codeGenerator.GenerateNextCodeAsync("BA-NT", "Admission", cancellationToken),
            PatientId = patientId,
            EncounterId = encounterId,
            AdmissionRequestId = admissionRequestId,
            FacilityId = facilityId,
            DepartmentId = departmentId,
            AttendingDoctorId = currentUserService.UserId,
            AdmittedAtUtc = DateTimeOffset.UtcNow,
            Status = AdmissionStatus.Active,
            AdmissionDiagnosis = admissionDiagnosis?.Trim()
        };

        db.Admissions.Add(admission);
        db.BedAssignments.Add(new BedAssignment
        {
            AdmissionId = admission.Id,
            BedId = bed.Id,
            DepartmentId = bed.DepartmentId,
            RoomId = bed.RoomId,
            AssignedAtUtc = DateTimeOffset.UtcNow,
            IsCurrent = true
        });

        BedStatus oldBedStatus = bed.Status;
        bed.Status = BedStatus.Occupied;
        db.BedStatusHistories.Add(new BedStatusHistory
        {
            BedId = bed.Id,
            FromStatus = oldBedStatus,
            ToStatus = BedStatus.Occupied,
            Reason = $"Nhập viện {admission.AdmissionCode}",
            ChangedBy = currentUserService.FullName
        });

        if (admissionRequestId.HasValue)
        {
            AdmissionRequest? req = await db.AdmissionRequests.FirstOrDefaultAsync(r => r.Id == admissionRequestId.Value, cancellationToken);
            if (req is not null) req.Status = AdmissionRequestStatus.Admitted;
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("PatientAdmitted", "Admission", admission.Id.ToString(),
            afterSummary: new { admission.AdmissionCode, Bed = bed.Code },
            facilityId: facilityId, departmentId: departmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(admission.Id, $"Đã tiếp nhận nội trú {admission.AdmissionCode} vào giường {bed.Code}.");
    }

    public async Task<OperationResult> TransferBedAsync(Guid admissionId, Guid newBedId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult.Fail("Phải nhập lý do chuyển giường.");

        Admission? admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId && a.Status == AdmissionStatus.Active, cancellationToken);
        if (admission is null) return OperationResult.Fail("Không tìm thấy đợt điều trị nội trú đang hoạt động.");

        Bed? newBed = await db.Beds.FirstOrDefaultAsync(b => b.Id == newBedId && b.FacilityId == admission.FacilityId, cancellationToken);
        if (newBed is null) return OperationResult.Fail("Không tìm thấy giường đích.");
        if (newBed.Status != BedStatus.Available)
            return OperationResult.Fail($"Giường {newBed.Code} hiện không trống ({newBed.Status}).");

        if (await db.BedAssignments.AnyAsync(ba => ba.BedId == newBedId && ba.IsCurrent, cancellationToken))
            return OperationResult.Fail($"Giường {newBed.Code} đang có bệnh nhân khác.");

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);

        BedAssignment? currentAssign = await db.BedAssignments.FirstOrDefaultAsync(ba => ba.AdmissionId == admissionId && ba.IsCurrent, cancellationToken);
        if (currentAssign is not null)
        {
            currentAssign.IsCurrent = false;
            currentAssign.ReleasedAtUtc = DateTimeOffset.UtcNow;
            currentAssign.TransferReason = reason.Trim();

            Bed? oldBed = await db.Beds.FirstOrDefaultAsync(b => b.Id == currentAssign.BedId, cancellationToken);
            if (oldBed is not null)
            {
                oldBed.Status = BedStatus.Cleaning;
                db.BedStatusHistories.Add(new BedStatusHistory
                {
                    BedId = oldBed.Id,
                    FromStatus = BedStatus.Occupied,
                    ToStatus = BedStatus.Cleaning,
                    Reason = $"Chuyển giường: {reason}",
                    ChangedBy = currentUserService.FullName
                });
            }
        }

        db.BedAssignments.Add(new BedAssignment
        {
            AdmissionId = admission.Id,
            BedId = newBed.Id,
            DepartmentId = newBed.DepartmentId,
            RoomId = newBed.RoomId,
            AssignedAtUtc = DateTimeOffset.UtcNow,
            IsCurrent = true
        });
        newBed.Status = BedStatus.Occupied;
        admission.DepartmentId = newBed.DepartmentId;

        db.BedStatusHistories.Add(new BedStatusHistory
        {
            BedId = newBed.Id,
            FromStatus = BedStatus.Available,
            ToStatus = BedStatus.Occupied,
            Reason = $"Chuyển đến từ đợt {admission.AdmissionCode}",
            ChangedBy = currentUserService.FullName
        });

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("BedTransferred", "Admission", admission.Id.ToString(), reason: reason,
            afterSummary: new { NewBed = newBed.Code }, facilityId: admission.FacilityId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã chuyển bệnh nhân sang giường {newBed.Code}.");
    }

    public async Task<OperationResult> DischargePatientAsync(Guid admissionId, string dischargeSummary, string? dischargeInstructions, DateOnly? followUpDate, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dischargeSummary)) return OperationResult.Fail("Tóm tắt ra viện là bắt buộc.");

        Admission? admission = await db.Admissions.FirstOrDefaultAsync(a => a.Id == admissionId && a.Status == AdmissionStatus.Active, cancellationToken);
        if (admission is null) return OperationResult.Fail("Không tìm thấy đợt điều trị nội trú đang hoạt động.");

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);

        admission.Status = AdmissionStatus.Discharged;
        admission.DischargedAtUtc = DateTimeOffset.UtcNow;
        admission.DischargeSummary = dischargeSummary.Trim();
        admission.DischargeInstructions = dischargeInstructions?.Trim();
        admission.FollowUpDate = followUpDate;

        BedAssignment? currentAssign = await db.BedAssignments.FirstOrDefaultAsync(ba => ba.AdmissionId == admissionId && ba.IsCurrent, cancellationToken);
        if (currentAssign is not null)
        {
            currentAssign.IsCurrent = false;
            currentAssign.ReleasedAtUtc = DateTimeOffset.UtcNow;

            Bed? bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == currentAssign.BedId, cancellationToken);
            if (bed is not null)
            {
                bed.Status = BedStatus.Cleaning;
                db.BedStatusHistories.Add(new BedStatusHistory
                {
                    BedId = bed.Id,
                    FromStatus = BedStatus.Occupied,
                    ToStatus = BedStatus.Cleaning,
                    Reason = $"Xuất viện {admission.AdmissionCode}",
                    ChangedBy = currentUserService.FullName
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("PatientDischarged", "Admission", admission.Id.ToString(),
            afterSummary: new { admission.AdmissionCode, admission.DischargedAtUtc },
            facilityId: admission.FacilityId, departmentId: admission.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã hoàn tất xuất viện {admission.AdmissionCode}. Giường đã chuyển sang trạng thái chờ vệ sinh.");
    }

    public async Task<OperationResult> UpdateBedStatusAsync(Guid bedId, BedStatus newStatus, string? reason, CancellationToken cancellationToken = default)
    {
        Bed? bed = await db.Beds.FirstOrDefaultAsync(b => b.Id == bedId, cancellationToken);
        if (bed is null) return OperationResult.Fail("Không tìm thấy giường.");

        if (bed.Status == BedStatus.Occupied && newStatus != BedStatus.Occupied)
        {
            if (await db.BedAssignments.AnyAsync(ba => ba.BedId == bedId && ba.IsCurrent, cancellationToken))
                return OperationResult.Fail("Giường đang có bệnh nhân nằm, hãy chuyển giường hoặc xuất viện trước.");
        }

        BedStatus oldStatus = bed.Status;
        bed.Status = newStatus;
        db.BedStatusHistories.Add(new BedStatusHistory
        {
            BedId = bed.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Reason = reason?.Trim(),
            ChangedBy = currentUserService.FullName
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Đã chuyển trạng thái giường {bed.Code} sang {newStatus}.");
    }

    public async Task<OperationResult> AddNursingNoteAsync(Guid admissionId, string noteText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(noteText)) return OperationResult.Fail("Nội dung phiếu chăm sóc là bắt buộc.");
        Admission? adm = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (adm is null) return OperationResult.Fail("Không tìm thấy hồ sơ nội trú.");

        db.NursingNotes.Add(new NursingNote
        {
            AdmissionId = admissionId,
            PatientId = adm.PatientId,
            RecordedAtUtc = DateTimeOffset.UtcNow,
            RecordedBy = currentUserService.FullName,
            NoteText = noteText.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã lưu phiếu chăm sóc điều dưỡng.");
    }

    public async Task<OperationResult> RecordNursingObservationAsync(Guid admissionId, int? sysBp, int? diaBp, int? hr, decimal? temp, int? spo2, string? fluidIo, CancellationToken cancellationToken = default)
    {
        Admission? adm = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (adm is null) return OperationResult.Fail("Không tìm thấy hồ sơ nội trú.");

        var alerts = new List<string>();
        if (sysBp is > 160 or < 90) alerts.Add($"HA tâm thu={sysBp}");
        if (temp is > 38.5m or < 35.5m) alerts.Add($"Nhiệt độ={temp}");
        if (spo2 is < 94) alerts.Add($"SpO2={spo2}%");
        if (hr is > 110 or < 50) alerts.Add($"Mạch={hr}");

        db.NursingObservations.Add(new NursingObservation
        {
            AdmissionId = admissionId,
            PatientId = adm.PatientId,
            ObservedAtUtc = DateTimeOffset.UtcNow,
            RecordedBy = currentUserService.FullName,
            SystolicBp = sysBp,
            DiastolicBp = diaBp,
            HeartRate = hr,
            TemperatureC = temp,
            SpO2 = spo2,
            FluidIntakeOutput = fluidIo?.Trim(),
            IsOutOfThreshold = alerts.Count > 0,
            AlertNote = alerts.Count > 0 ? $"Ngoài ngưỡng cấu hình: {string.Join(", ", alerts)}" : null
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok(alerts.Count > 0 ? $"Đã lưu theo dõi (Cảnh báo: {string.Join(", ", alerts)})." : "Đã lưu chỉ số theo dõi điều dưỡng.");
    }

    public async Task<OperationResult> RecordMedicationAdministrationAsync(Guid admissionId, string medicineName, string dose, string status, string? omissionReason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(medicineName)) return OperationResult.Fail("Tên thuốc là bắt buộc.");
        if (status == "Omitted" && string.IsNullOrWhiteSpace(omissionReason))
            return OperationResult.Fail("Phải nhập lý do khi bỏ liều thuốc.");

        Admission? adm = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (adm is null) return OperationResult.Fail("Không tìm thấy hồ sơ nội trú.");

        db.MedicationAdministrations.Add(new MedicationAdministration
        {
            AdmissionId = admissionId,
            PatientId = adm.PatientId,
            MedicineName = medicineName.Trim(),
            Dose = dose.Trim(),
            ScheduledAtUtc = DateTimeOffset.UtcNow,
            AdministeredAtUtc = status == "Administered" ? DateTimeOffset.UtcNow : null,
            Status = status,
            OmissionReason = omissionReason?.Trim(),
            RecordedBy = currentUserService.FullName
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã ghi nhận thực hiện y lệnh thuốc.");
    }

    public async Task<OperationResult> AddShiftHandoverAsync(Guid facilityId, Guid departmentId, string shiftName, string handoverTo, string summary, CancellationToken cancellationToken = default)
    {
        db.ShiftHandoverNotes.Add(new ShiftHandoverNote
        {
            FacilityId = facilityId,
            DepartmentId = departmentId,
            ShiftDate = HospitalTime.TodayVietnam(),
            ShiftName = shiftName.Trim(),
            HandoverFrom = currentUserService.FullName,
            HandoverTo = handoverTo.Trim(),
            Summary = summary.Trim()
        });
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã lưu biên bản bàn giao ca trực.");
    }

    public async Task<IReadOnlyList<NursingNote>> GetNursingNotesAsync(Guid admissionId, CancellationToken cancellationToken = default)
        => await db.NursingNotes.AsNoTracking().Where(n => n.AdmissionId == admissionId).OrderByDescending(n => n.RecordedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NursingObservation>> GetNursingObservationsAsync(Guid admissionId, CancellationToken cancellationToken = default)
        => await db.NursingObservations.AsNoTracking().Where(n => n.AdmissionId == admissionId).OrderByDescending(n => n.ObservedAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MedicationAdministration>> GetMedicationAdministrationsAsync(Guid admissionId, CancellationToken cancellationToken = default)
        => await db.MedicationAdministrations.AsNoTracking().Where(n => n.AdmissionId == admissionId).OrderByDescending(n => n.ScheduledAtUtc).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<EmergencyIntake>> GetEmergencyIntakesAsync(Guid facilityId, CancellationToken cancellationToken = default)
        => await db.EmergencyIntakes.AsNoTracking().Where(e => e.FacilityId == facilityId).OrderBy(e => e.TriageLevel).ThenByDescending(e => e.ArrivedAtUtc).ToListAsync(cancellationToken);

    public async Task<OperationResult<Guid>> CreateEmergencyIntakeAsync(Guid patientId, Guid facilityId, EmergencyTriageLevel triageLevel, string initialCondition, string? bayLocation, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(initialCondition)) return OperationResult<Guid>.Fail("Tình trạng ban đầu là bắt buộc.");

        Department emergDept = await db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.FacilityId == facilityId && d.DepartmentType == DepartmentType.Emergency, cancellationToken)
            ?? await db.Departments.AsNoTracking().FirstAsync(d => d.FacilityId == facilityId, cancellationToken);

        await using IDbContextTransaction tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var encounter = new Encounter
        {
            EncounterCode = await codeGenerator.GenerateNextCodeAsync("CC-LK", "Encounter", cancellationToken),
            PatientId = patientId,
            FacilityId = facilityId,
            DepartmentId = emergDept.Id,
            EncounterType = EncounterType.Emergency,
            Status = EncounterStatus.InProgress,
            StartedAtUtc = DateTimeOffset.UtcNow,
            ChiefComplaint = initialCondition.Trim(),
            IsEmergency = true
        };
        db.Encounters.Add(encounter);

        var intake = new EmergencyIntake
        {
            EmergencyCode = await codeGenerator.GenerateNextCodeAsync("CC", "Emergency", cancellationToken),
            PatientId = patientId,
            EncounterId = encounter.Id,
            FacilityId = facilityId,
            ArrivedAtUtc = DateTimeOffset.UtcNow,
            TriageLevel = triageLevel,
            TriagedBy = currentUserService.FullName,
            InitialCondition = initialCondition.Trim(),
            BayLocation = bayLocation?.Trim(),
            Status = "Active"
        };
        db.EmergencyIntakes.Add(intake);

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("EmergencyIntakeCreated", "EmergencyIntake", intake.Id.ToString(),
            afterSummary: new { intake.EmergencyCode, Triage = triageLevel.ToString() }, facilityId: facilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(intake.Id, $"Đã tiếp nhận cấp cứu {intake.EmergencyCode} (Ưu tiên: {triageLevel}).");
    }

    public async Task<IReadOnlyList<SurgeryRequest>> GetSurgeryRequestsAsync(Guid facilityId, CancellationToken cancellationToken = default)
        => await db.SurgeryRequests.AsNoTracking().Where(s => s.FacilityId == facilityId).OrderByDescending(s => s.CreatedAtUtc).ToListAsync(cancellationToken);

    public async Task<OperationResult<Guid>> CreateSurgeryRequestAsync(Guid patientId, Guid encounterId, Guid facilityId, string procedureName, DateTimeOffset? scheduledAtUtc, Guid? operatingRoomId, string? teamSummary, string? checklistSummary, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(procedureName)) return OperationResult<Guid>.Fail("Tên phẫu thuật/thủ thuật là bắt buộc.");

        var req = new SurgeryRequest
        {
            RequestCode = await codeGenerator.GenerateNextCodeAsync("PTM", "Surgery", cancellationToken),
            PatientId = patientId,
            EncounterId = encounterId,
            FacilityId = facilityId,
            ProcedureName = procedureName.Trim(),
            RequestedByDoctorId = currentUserService.UserId ?? Guid.Empty,
            ScheduledAtUtc = scheduledAtUtc?.ToUniversalTime(),
            OperatingRoomId = operatingRoomId,
            TeamSummary = teamSummary?.Trim(),
            ChecklistSummary = checklistSummary?.Trim(),
            Status = scheduledAtUtc.HasValue ? SurgeryStatus.Scheduled : SurgeryStatus.Requested
        };
        db.SurgeryRequests.Add(req);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(req.Id, $"Đã tạo yêu cầu phẫu thuật {req.RequestCode}.");
    }

    public async Task<OperationResult> UpdateSurgeryStatusAsync(Guid surgeryId, SurgeryStatus status, string? operativeNote, string? suppliesUsed, CancellationToken cancellationToken = default)
    {
        SurgeryRequest? req = await db.SurgeryRequests.FirstOrDefaultAsync(s => s.Id == surgeryId, cancellationToken);
        if (req is null) return OperationResult.Fail("Không tìm thấy yêu cầu phẫu thuật.");

        req.Status = status;
        if (!string.IsNullOrWhiteSpace(operativeNote)) req.OperativeNote = operativeNote.Trim();
        if (!string.IsNullOrWhiteSpace(suppliesUsed)) req.SuppliesUsedSummary = suppliesUsed.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Đã cập nhật trạng thái phẫu thuật sang {status}.");
    }

    public async Task<IReadOnlyList<Employee>> GetEmployeesAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Employee> q = db.Employees.AsNoTracking().Include(e => e.Department);
        if (facilityId.HasValue) q = q.Where(e => e.FacilityId == facilityId.Value);
        return await q.OrderBy(e => e.EmployeeCode).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Guid>> CreateEmployeeAsync(Guid facilityId, Guid? departmentId, string employeeCode, string fullName, string? jobTitle, string? phone, string? email, DateOnly? hireDate, CancellationToken cancellationToken = default)
    {
        employeeCode = employeeCode.Trim().ToUpperInvariant();
        if (await db.Employees.AnyAsync(e => e.EmployeeCode == employeeCode, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã nhân viên '{employeeCode}' đã tồn tại.");

        var emp = new Employee
        {
            FacilityId = facilityId, DepartmentId = departmentId, EmployeeCode = employeeCode,
            FullName = fullName.Trim(), JobTitle = jobTitle?.Trim(), Phone = phone?.Trim(),
            Email = email?.Trim(), HireDate = hireDate
        };
        db.Employees.Add(emp);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EmployeeCreated", "Employee", emp.Id.ToString(), afterSummary: new { emp.EmployeeCode, emp.FullName }, facilityId: facilityId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(emp.Id, $"Đã thêm nhân viên {emp.EmployeeCode}.");
    }

    public async Task<OperationResult> UpdateEmployeeAsync(Guid employeeId, Guid? departmentId, string fullName, string? jobTitle, string? phone, string? email, bool isActive, bool isTerminated, CancellationToken cancellationToken = default)
    {
        var emp = await db.Employees.FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (emp is null) return OperationResult.Fail("Không tìm thấy nhân viên.");
        emp.DepartmentId = departmentId;
        emp.FullName = fullName.Trim();
        emp.JobTitle = jobTitle?.Trim();
        emp.Phone = phone?.Trim();
        emp.Email = email?.Trim();
        emp.IsActive = isActive && !isTerminated;
        emp.IsTerminated = isTerminated;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("EmployeeUpdated", "Employee", emp.Id.ToString(), afterSummary: new { emp.FullName, emp.IsActive, emp.IsTerminated }, facilityId: emp.FacilityId, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật hồ sơ nhân viên.");
    }

    public async Task<IReadOnlyList<ShiftSchedule>> GetShiftSchedulesAsync(Guid facilityId, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        IQueryable<ShiftSchedule> q = db.ShiftSchedules.AsNoTracking().Include(s => s.Employee).Where(s => s.FacilityId == facilityId);
        if (date.HasValue) q = q.Where(s => s.ShiftDate == date.Value);
        return await q.OrderByDescending(s => s.ShiftDate).ThenBy(s => s.StartTime).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Guid>> CreateShiftScheduleAsync(Guid facilityId, Guid departmentId, Guid employeeId, Guid? clinicId, DateOnly shiftDate, string shiftName, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken = default)
    {
        var item = new ShiftSchedule
        {
            FacilityId = facilityId,
            DepartmentId = departmentId,
            EmployeeId = employeeId,
            ClinicId = clinicId,
            ShiftDate = shiftDate,
            ShiftName = shiftName.Trim(),
            StartTime = startTime,
            EndTime = endTime
        };
        db.ShiftSchedules.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(item.Id, "Đã phân ca trực thành công.");
    }

    public async Task<OperationResult> DeleteShiftScheduleAsync(Guid shiftId, CancellationToken cancellationToken = default)
    {
        var shift = await db.ShiftSchedules.FirstOrDefaultAsync(s => s.Id == shiftId, cancellationToken);
        if (shift is null) return OperationResult.Fail("Không tìm thấy ca trực.");
        db.ShiftSchedules.Remove(shift);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã xóa ca trực.");
    }

    public async Task<IReadOnlyList<LeaveRecord>> GetLeaveRecordsAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        return await (
            from l in db.LeaveRecords.AsNoTracking().Include(x => x.Employee)
            where l.Employee != null && l.Employee.FacilityId == facilityId
            orderby l.FromDate descending
            select l).Take(50).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Guid>> CreateLeaveRecordAsync(Guid employeeId, DateOnly fromDate, DateOnly toDate, string leaveType, string reason, CancellationToken cancellationToken = default)
    {
        if (toDate < fromDate) return OperationResult<Guid>.Fail("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
        var item = new LeaveRecord
        {
            EmployeeId = employeeId, FromDate = fromDate, ToDate = toDate,
            LeaveType = leaveType.Trim(), Reason = reason.Trim(), Status = "Approved"
        };
        db.LeaveRecords.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(item.Id, "Đã ghi nhận ngày nghỉ phép.");
    }

    public async Task<DashboardMetricsDto> GetDashboardMetricsAsync(Guid facilityId, CancellationToken cancellationToken = default)
    {
        DateOnly today = HospitalTime.TodayVietnam();
        DateTimeOffset startUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
        DateTimeOffset endUtc = new DateTimeOffset(today.ToDateTime(TimeOnly.MaxValue), TimeSpan.FromHours(7)).ToUniversalTime();

        var encountersToday = await db.Encounters.CountAsync(e => e.FacilityId == facilityId && e.StartedAtUtc >= startUtc && e.StartedAtUtc <= endUtc, cancellationToken);
        var waitingQueue = await db.QueueTickets.CountAsync(t => t.FacilityId == facilityId && t.QueueDate == today && (t.Status == QueueTicketStatus.Waiting || t.Status == QueueTicketStatus.Called), cancellationToken);
        var activeAdmissions = await db.Admissions.CountAsync(a => a.FacilityId == facilityId && a.Status == AdmissionStatus.Active, cancellationToken);
        var totalBeds = await db.Beds.CountAsync(b => b.FacilityId == facilityId && b.IsActive, cancellationToken);
        var occupiedBeds = await db.Beds.CountAsync(b => b.FacilityId == facilityId && b.IsActive && b.Status == BedStatus.Occupied, cancellationToken);
        var pendingLabOrders = await db.LabOrders.CountAsync(o => o.FacilityId == facilityId && o.Status != LabOrderStatus.Verified && o.Status != LabOrderStatus.Cancelled && o.Status != LabOrderStatus.Rejected, cancellationToken);
        var pendingLabVerify = await db.LabOrders.CountAsync(o => o.FacilityId == facilityId && o.Status == LabOrderStatus.PendingReview, cancellationToken);
        var pendingPrescriptions = await db.Prescriptions.CountAsync(p => p.FacilityId == facilityId && (p.Status == PrescriptionStatus.Submitted || p.Status == PrescriptionStatus.PartiallyDispensed), cancellationToken);
        var revenueToday = await db.Payments.Where(p => p.FacilityId == facilityId && p.PaidAtUtc >= startUtc && p.PaidAtUtc <= endUtc).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        var lowStock = await (from b in db.MedicineBatches
                              join m in db.Medicines on b.MedicineId equals m.Id
                              where b.IsActive && b.QuantityOnHand <= m.MinStockLevel
                              select b.Id).CountAsync(cancellationToken);

        return new DashboardMetricsDto(
            encountersToday, waitingQueue, activeAdmissions, totalBeds, occupiedBeds,
            pendingLabOrders, pendingLabVerify, pendingPrescriptions, revenueToday, lowStock);
    }
}
