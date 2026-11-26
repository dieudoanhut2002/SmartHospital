using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Reception;

namespace SmartHospital.Application.Reception;

public record AppointmentListItemDto(
    Guid Id,
    string AppointmentCode,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    string? PhoneNumber,
    Guid FacilityId,
    Guid DepartmentId,
    string DepartmentName,
    Guid? ClinicId,
    string? ClinicName,
    DateTimeOffset ScheduledAtUtc,
    AppointmentStatus Status,
    string? ChiefComplaint,
    Guid? EncounterId);

public record CreateAppointmentRequest(
    Guid PatientId,
    Guid FacilityId,
    Guid DepartmentId,
    Guid? SpecialtyId,
    Guid? ClinicId,
    Guid? DoctorId,
    DateTimeOffset ScheduledAtUtc,
    string? ChiefComplaint,
    string? Notes);

public record WalkInOrCheckInRequest(
    Guid PatientId,
    Guid FacilityId,
    Guid DepartmentId,
    Guid ClinicId,
    Guid? SpecialtyId,
    Guid? DoctorId,
    Guid? AppointmentId,
    EncounterType EncounterType,
    string? ChiefComplaint,
    int Priority = 0);

public record QueueTicketDto(
    Guid TicketId,
    Guid EncounterId,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    Guid ClinicId,
    string ClinicName,
    string TicketNumber,
    int SequenceNumber,
    int Priority,
    QueueTicketStatus TicketStatus,
    EncounterStatus EncounterStatus,
    DateTimeOffset IssuedAtUtc);

public interface IReceptionService
{
    Task<PagedResult<AppointmentListItemDto>> GetAppointmentsAsync(PagedRequest request, Guid? facilityId = null, AppointmentStatus? status = null, DateOnly? date = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> RescheduleAppointmentAsync(Guid appointmentId, DateTimeOffset newScheduledAtUtc, Guid? clinicId, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus newStatus, string? reason, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateEncounterWithQueueAsync(WalkInOrCheckInRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<QueueTicketDto>> GetQueueBoardAsync(Guid facilityId, Guid? clinicId = null, DateOnly? date = null, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateQueueTicketStatusAsync(Guid ticketId, QueueTicketStatus newStatus, Guid? transferToClinicId = null, string? notes = null, CancellationToken cancellationToken = default);
}

public class ReceptionService(
    IHospitalDbContext db,
    ICodeGeneratorService codeGenerator,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    INotificationProvider notificationProvider) : IReceptionService
{
    public async Task<PagedResult<AppointmentListItemDto>> GetAppointmentsAsync(
        PagedRequest request,
        Guid? facilityId = null,
        AppointmentStatus? status = null,
        DateOnly? date = null,
        CancellationToken cancellationToken = default)
    {
        var query = from a in db.Appointments.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on a.PatientId equals p.Id
                    join d in db.Departments.AsNoTracking() on a.DepartmentId equals d.Id
                    join c in db.Clinics.AsNoTracking() on a.ClinicId equals c.Id into cj
                    from c in cj.DefaultIfEmpty()
                    select new { a, p, d, c };

        if (facilityId.HasValue) query = query.Where(x => x.a.FacilityId == facilityId.Value);
        if (status.HasValue) query = query.Where(x => x.a.Status == status.Value);
        if (date.HasValue)
        {
            var startUtc = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            var endUtc = new DateTimeOffset(date.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.FromHours(7)).ToUniversalTime();
            query = query.Where(x => x.a.ScheduledAtUtc >= startUtc && x.a.ScheduledAtUtc <= endUtc);
        }
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x =>
                x.a.AppointmentCode.Contains(term) ||
                x.p.PatientCode.Contains(term) ||
                x.p.FullName.Contains(term) ||
                (x.p.PhoneNumber != null && x.p.PhoneNumber.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.a.ScheduledAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new AppointmentListItemDto(
                x.a.Id, x.a.AppointmentCode, x.p.Id, x.p.PatientCode, x.p.FullName, x.p.PhoneNumber,
                x.a.FacilityId, x.a.DepartmentId, x.d.Name, x.a.ClinicId, x.c != null ? x.c.Name : null,
                x.a.ScheduledAtUtc, x.a.Status, x.a.ChiefComplaint, x.a.EncounterId))
            .ToListAsync(cancellationToken);

        return new PagedResult<AppointmentListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<OperationResult<Guid>> CreateAppointmentAsync(CreateAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PatientId && !p.IsMerged, cancellationToken);
        if (patient is null) return OperationResult<Guid>.Fail("Không tìm thấy hồ sơ bệnh nhân.");

        var appt = new Appointment
        {
            AppointmentCode = await codeGenerator.GenerateNextCodeAsync("LH", "Appointment", cancellationToken),
            PatientId = request.PatientId,
            FacilityId = request.FacilityId,
            DepartmentId = request.DepartmentId,
            SpecialtyId = request.SpecialtyId,
            ClinicId = request.ClinicId,
            DoctorId = request.DoctorId,
            ScheduledAtUtc = request.ScheduledAtUtc.ToUniversalTime(),
            Status = AppointmentStatus.Booked,
            ChiefComplaint = request.ChiefComplaint?.Trim(),
            Notes = request.Notes?.Trim()
        };

        db.Appointments.Add(appt);
        await db.SaveChangesAsync(cancellationToken);

        await notificationProvider.SendNotificationAsync(
            null, patient.Id, "InApp",
            "Đặt lịch khám thành công",
            $"Mã lịch hẹn {appt.AppointmentCode} lúc {HospitalTime.ToVietnamTime(appt.ScheduledAtUtc):dd/MM/yyyy HH:mm}.",
            cancellationToken: cancellationToken);

        await auditService.LogAsync("AppointmentCreated", "Appointment", appt.Id.ToString(),
            afterSummary: new { appt.AppointmentCode, appt.PatientId, appt.ScheduledAtUtc },
            facilityId: request.FacilityId, departmentId: request.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(appt.Id, $"Đã tạo lịch hẹn {appt.AppointmentCode}.");
    }

    public async Task<OperationResult> RescheduleAppointmentAsync(Guid appointmentId, DateTimeOffset newScheduledAtUtc, Guid? clinicId, string? notes, CancellationToken cancellationToken = default)
    {
        var appt = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
        if (appt is null) return OperationResult.Fail("Không tìm thấy lịch hẹn.");
        if (appt.Status != AppointmentStatus.Booked)
            return OperationResult.Fail($"Chỉ được đổi lịch khi lịch hẹn đang ở trạng thái Booked (hiện tại: {appt.Status}).");

        appt.ScheduledAtUtc = newScheduledAtUtc.ToUniversalTime();
        if (clinicId.HasValue) appt.ClinicId = clinicId.Value;
        if (!string.IsNullOrWhiteSpace(notes)) appt.Notes = notes.Trim();
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("AppointmentRescheduled", "Appointment", appt.Id.ToString(),
            afterSummary: new { appt.AppointmentCode, appt.ScheduledAtUtc }, facilityId: appt.FacilityId, cancellationToken: cancellationToken);
        return OperationResult.Ok($"Đã cập nhật lịch hẹn {appt.AppointmentCode}.");
    }

    public async Task<OperationResult> UpdateAppointmentStatusAsync(Guid appointmentId, AppointmentStatus newStatus, string? reason, CancellationToken cancellationToken = default)
    {
        var appt = await db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId, cancellationToken);
        if (appt is null) return OperationResult.Fail("Không tìm thấy lịch hẹn.");

        if (appt.Status is AppointmentStatus.Completed or AppointmentStatus.Cancelled)
            return OperationResult.Fail($"Lịch hẹn đang ở trạng thái {appt.Status}, không thể chuyển tiếp.");

        if (newStatus == AppointmentStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
            return OperationResult.Fail("Phải nhập lý do khi hủy lịch hẹn.");

        var oldStatus = appt.Status;
        appt.Status = newStatus;
        appt.CancelReason = reason?.Trim();
        await db.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync("AppointmentStatusChanged", "Appointment", appt.Id.ToString(), reason: reason,
            beforeSummary: new { Status = oldStatus.ToString() },
            afterSummary: new { Status = newStatus.ToString() },
            facilityId: appt.FacilityId, departmentId: appt.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã cập nhật trạng thái lịch hẹn sang {newStatus}.");
    }

    public async Task<OperationResult<Guid>> CreateEncounterWithQueueAsync(WalkInOrCheckInRequest request, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.PatientId && !p.IsMerged, cancellationToken);
        if (patient is null) return OperationResult<Guid>.Fail("Không tìm thấy bệnh nhân hợp lệ.");

        var clinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClinicId && c.FacilityId == request.FacilityId, cancellationToken);
        if (clinic is null) return OperationResult<Guid>.Fail("Phòng khám không thuộc cơ sở đã chọn.");

        Appointment? appointment = null;
        if (request.AppointmentId.HasValue)
        {
            appointment = await db.Appointments.FirstOrDefaultAsync(a => a.Id == request.AppointmentId.Value, cancellationToken);
            if (appointment is null) return OperationResult<Guid>.Fail("Không tìm thấy lịch hẹn liên kết.");
            if (appointment.EncounterId.HasValue) return OperationResult<Guid>.Fail("Lịch hẹn này đã được check-in tạo lượt khám trước đó.");
            if (appointment.Status is AppointmentStatus.Cancelled or AppointmentStatus.NoShow)
                return OperationResult<Guid>.Fail("Không thể tiếp nhận lịch hẹn đã hủy hoặc vắng mặt.");
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var todayVn = HospitalTime.TodayVietnam();
        var maxSeq = await db.QueueTickets
            .Where(t => t.FacilityId == request.FacilityId && t.ClinicId == request.ClinicId && t.QueueDate == todayVn)
            .Select(t => (int?)t.SequenceNumber)
            .MaxAsync(cancellationToken) ?? 0;
        var nextSeq = maxSeq + 1;
        var ticketNumber = $"{clinic.QueuePrefix}-{nextSeq:D3}";

        var encounter = new Encounter
        {
            EncounterCode = await codeGenerator.GenerateNextCodeAsync("LK", "Encounter", cancellationToken),
            PatientId = patient.Id,
            FacilityId = request.FacilityId,
            DepartmentId = request.DepartmentId,
            SpecialtyId = request.SpecialtyId ?? clinic.SpecialtyId,
            ClinicId = clinic.Id,
            DoctorId = request.DoctorId,
            AppointmentId = appointment?.Id,
            EncounterType = request.EncounterType,
            Status = EncounterStatus.Waiting,
            StartedAtUtc = DateTimeOffset.UtcNow,
            ChiefComplaint = request.ChiefComplaint?.Trim() ?? appointment?.ChiefComplaint,
            IsEmergency = request.EncounterType == EncounterType.Emergency
        };

        var ticket = new QueueTicket
        {
            EncounterId = encounter.Id,
            FacilityId = request.FacilityId,
            ClinicId = clinic.Id,
            QueueDate = todayVn,
            SequenceNumber = nextSeq,
            TicketNumber = ticketNumber,
            Priority = request.Priority,
            Status = QueueTicketStatus.Waiting,
            IssuedAtUtc = DateTimeOffset.UtcNow
        };

        db.Encounters.Add(encounter);
        db.QueueTickets.Add(ticket);
        db.EncounterStatusHistories.Add(new EncounterStatusHistory
        {
            EncounterId = encounter.Id,
            FromStatus = appointment is not null ? EncounterStatus.Booked : EncounterStatus.CheckedIn,
            ToStatus = EncounterStatus.Waiting,
            ChangedBy = currentUserService.UserName,
            Reason = appointment is not null ? $"Check-in từ lịch hẹn {appointment.AppointmentCode}" : "Tiếp nhận trực tiếp"
        });

        if (appointment is not null)
        {
            appointment.Status = AppointmentStatus.Waiting;
            appointment.EncounterId = encounter.Id;
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("EncounterStarted", "Encounter", encounter.Id.ToString(),
            afterSummary: new { encounter.EncounterCode, ticket.TicketNumber, patient.PatientCode },
            facilityId: request.FacilityId, departmentId: request.DepartmentId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(encounter.Id, $"Đã tiếp nhận lượt khám {encounter.EncounterCode} - Số thứ tự: {ticketNumber}.");
    }

    public async Task<IReadOnlyList<QueueTicketDto>> GetQueueBoardAsync(Guid facilityId, Guid? clinicId = null, DateOnly? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date ?? HospitalTime.TodayVietnam();

        var query = from t in db.QueueTickets.AsNoTracking()
                    join e in db.Encounters.AsNoTracking() on t.EncounterId equals e.Id
                    join p in db.Patients.AsNoTracking() on e.PatientId equals p.Id
                    join c in db.Clinics.AsNoTracking() on t.ClinicId equals c.Id
                    where t.FacilityId == facilityId && t.QueueDate == targetDate
                    select new { t, e, p, c };

        if (clinicId.HasValue)
        {
            query = query.Where(x => x.t.ClinicId == clinicId.Value);
        }

        return await query
            .OrderByDescending(x => x.t.Priority)
            .ThenBy(x => x.t.SequenceNumber)
            .Select(x => new QueueTicketDto(
                x.t.Id, x.e.Id, x.p.Id, x.p.PatientCode, x.p.FullName,
                x.c.Id, x.c.Name, x.t.TicketNumber, x.t.SequenceNumber, x.t.Priority,
                x.t.Status, x.e.Status, x.t.IssuedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<OperationResult> UpdateQueueTicketStatusAsync(
        Guid ticketId,
        QueueTicketStatus newStatus,
        Guid? transferToClinicId = null,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var ticket = await db.QueueTickets.FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null) return OperationResult.Fail("Không tìm thấy phiếu số thứ tự.");

        var encounter = await db.Encounters.FirstOrDefaultAsync(e => e.Id == ticket.EncounterId, cancellationToken);
        if (encounter is null) return OperationResult.Fail("Không tìm thấy lượt khám liên kết.");

        var now = DateTimeOffset.UtcNow;

        if (newStatus == QueueTicketStatus.Transferred)
        {
            if (!transferToClinicId.HasValue || transferToClinicId.Value == ticket.ClinicId)
                return OperationResult.Fail("Vui lòng chọn phòng khám đích khác để chuyển hàng đợi.");

            var targetClinic = await db.Clinics.AsNoTracking().FirstOrDefaultAsync(c => c.Id == transferToClinicId.Value && c.FacilityId == ticket.FacilityId, cancellationToken);
            if (targetClinic is null) return OperationResult.Fail("Phòng khám đích không hợp lệ.");

            await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
            ticket.Status = QueueTicketStatus.Transferred;
            ticket.Notes = notes?.Trim();

            var todayVn = HospitalTime.TodayVietnam();
            var nextSeq = (await db.QueueTickets
                .Where(t => t.FacilityId == ticket.FacilityId && t.ClinicId == targetClinic.Id && t.QueueDate == todayVn)
                .Select(t => (int?)t.SequenceNumber)
                .MaxAsync(cancellationToken) ?? 0) + 1;

            var newTicket = new QueueTicket
            {
                EncounterId = encounter.Id,
                FacilityId = ticket.FacilityId,
                ClinicId = targetClinic.Id,
                QueueDate = todayVn,
                SequenceNumber = nextSeq,
                TicketNumber = $"{targetClinic.QueuePrefix}-{nextSeq:D3}",
                Priority = ticket.Priority,
                Status = QueueTicketStatus.Waiting,
                IssuedAtUtc = now,
                Notes = $"Chuyển từ {ticket.TicketNumber}"
            };

            encounter.ClinicId = targetClinic.Id;
            encounter.DepartmentId = targetClinic.DepartmentId;
            encounter.Status = EncounterStatus.Waiting;
            db.QueueTickets.Add(newTicket);
            await db.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            await auditService.LogAsync("QueueTicketTransferred", "QueueTicket", ticket.Id.ToString(),
                afterSummary: new { OldTicket = ticket.TicketNumber, NewTicket = newTicket.TicketNumber },
                facilityId: ticket.FacilityId, cancellationToken: cancellationToken);

            return OperationResult.Ok($"Đã chuyển sang phòng {targetClinic.Name}, số mới: {newTicket.TicketNumber}.");
        }

        ticket.Status = newStatus;
        ticket.Notes = notes?.Trim() ?? ticket.Notes;

        if (newStatus == QueueTicketStatus.Called)
        {
            ticket.CalledAtUtc = now;
        }
        else if (newStatus == QueueTicketStatus.InProgress)
        {
            ticket.CalledAtUtc ??= now;
            if (encounter.Status == EncounterStatus.Waiting)
            {
                db.EncounterStatusHistories.Add(new EncounterStatusHistory
                {
                    EncounterId = encounter.Id,
                    FromStatus = encounter.Status,
                    ToStatus = EncounterStatus.InProgress,
                    ChangedBy = currentUserService.UserName
                });
                encounter.Status = EncounterStatus.InProgress;
            }
        }
        else if (newStatus == QueueTicketStatus.Completed)
        {
            ticket.CompletedAtUtc = now;
        }
        else if (newStatus == QueueTicketStatus.Cancelled)
        {
            encounter.Status = EncounterStatus.Cancelled;
            encounter.CancelReason = notes;
        }

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Đã cập nhật số {ticket.TicketNumber} sang {newStatus}.");
    }
}
