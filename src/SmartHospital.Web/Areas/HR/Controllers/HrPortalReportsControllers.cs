using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Reception;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.HR.Controllers
{
    [Area("HR")]
    [Authorize]
    public class HrController(
        IInpatientAndHospitalOpsService opsService,
        IMasterDataService masterDataService,
        ICurrentUserService currentUser) : Controller
    {
        [HasPermission(PermissionCodes.HrManage)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
            ViewBag.Employees = await opsService.GetEmployeesAsync(facId, cancellationToken);
            ViewBag.Shifts = await opsService.GetShiftSchedulesAsync(facId, null, cancellationToken);
            ViewBag.Leaves = await opsService.GetLeaveRecordsAsync(facId, cancellationToken);
            ViewBag.Departments = await masterDataService.GetDepartmentsAsync(facId, cancellationToken);
            ViewBag.Clinics = await masterDataService.GetClinicsAsync(facId, cancellationToken);
            ViewBag.ActiveFacilityId = facId;
            return View();
        }

        [HasPermission(PermissionCodes.HrManage)]
        [HttpPost]
        public async Task<IActionResult> CreateEmployee(Guid facilityId, Guid? departmentId, string employeeCode, string fullName, string? jobTitle, string? phone, string? email, DateOnly? hireDate, CancellationToken cancellationToken)
        {
            var res = await opsService.CreateEmployeeAsync(facilityId, departmentId, employeeCode, fullName, jobTitle, phone, email, hireDate, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.HrManage)]
        [HttpPost]
        public async Task<IActionResult> UpdateEmployee(Guid employeeId, Guid? departmentId, string fullName, string? jobTitle, string? phone, string? email, bool isActive, bool isTerminated, CancellationToken cancellationToken)
        {
            var res = await opsService.UpdateEmployeeAsync(employeeId, departmentId, fullName, jobTitle, phone, email, isActive, isTerminated, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.HrManage)]
        [HttpPost]
        public async Task<IActionResult> CreateShift(Guid facilityId, Guid departmentId, Guid employeeId, Guid? clinicId, DateOnly shiftDate, string shiftName, TimeOnly startTime, TimeOnly endTime, CancellationToken cancellationToken)
        {
            var res = await opsService.CreateShiftScheduleAsync(facilityId, departmentId, employeeId, clinicId, shiftDate, shiftName, startTime, endTime, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.HrManage)]
        [HttpPost]
        public async Task<IActionResult> DeleteShift(Guid shiftId, CancellationToken cancellationToken)
        {
            var res = await opsService.DeleteShiftScheduleAsync(shiftId, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.HrManage)]
        [HttpPost]
        public async Task<IActionResult> CreateLeave(Guid employeeId, DateOnly fromDate, DateOnly toDate, string leaveType, string reason, CancellationToken cancellationToken)
        {
            var res = await opsService.CreateLeaveRecordAsync(employeeId, fromDate, toDate, leaveType, reason, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartHospital.Web.Areas.Portal.Controllers
{
    [Area("Portal")]
    [Authorize]
    public class PortalController(
        IHospitalDbContext db,
        ICurrentUserService currentUser,
        IReceptionService receptionService,
        IPatientService patientService,
        IMasterDataService masterDataService) : Controller
    {
        [HasPermission(PermissionCodes.PortalAccess)]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PortalUserId == userId && !p.IsMerged, cancellationToken);

            ViewBag.Patient = patient;
            ViewBag.Facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
            ViewBag.Departments = await masterDataService.GetDepartmentsAsync(null, cancellationToken);
            ViewBag.Clinics = await masterDataService.GetClinicsAsync(null, cancellationToken);

            if (patient is not null)
            {
                ViewBag.Appointments = await db.Appointments.AsNoTracking().Where(a => a.PatientId == patient.Id).OrderByDescending(a => a.ScheduledAtUtc).Take(20).ToListAsync(cancellationToken);
                ViewBag.Prescriptions = await db.Prescriptions.AsNoTracking().Include(p => p.Items).Where(p => p.PatientId == patient.Id).OrderByDescending(p => p.PrescribedAtUtc).Take(20).ToListAsync(cancellationToken);
                ViewBag.Invoices = await db.Invoices.AsNoTracking().Where(i => i.PatientId == patient.Id).OrderByDescending(i => i.IssuedAtUtc).Take(20).ToListAsync(cancellationToken);
                ViewBag.LabResults = await (
                    from r in db.LabResults.AsNoTracking()
                    join i in db.LabOrderItems.AsNoTracking() on r.LabOrderItemId equals i.Id
                    join o in db.LabOrders.AsNoTracking() on i.LabOrderId equals o.Id
                    where o.PatientId == patient.Id && r.IsVerified && r.IsReleasedToPortal
                    orderby r.VerifiedAtUtc descending
                    select new { o.OrderCode, i.TestName, r.ResultValue, r.Unit, r.ReferenceRange, r.IsAbnormal, r.VerifiedAtUtc })
                    .Take(30).ToListAsync(cancellationToken);
            }

            return View();
        }

        [HasPermission(PermissionCodes.PortalAccess)]
        [HttpPost]
        public async Task<IActionResult> BookAppointment(Guid facilityId, Guid departmentId, Guid? clinicId, DateTime scheduledLocal, string? chiefComplaint, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PortalUserId == userId && !p.IsMerged, cancellationToken);
            if (patient is null)
            {
                TempData["Error"] = "Tài khoản chưa được liên kết với hồ sơ bệnh nhân.";
                return RedirectToAction(nameof(Index));
            }

            var scheduledUtc = new DateTimeOffset(DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToUniversalTime();
            var res = await receptionService.CreateAppointmentAsync(new CreateAppointmentRequest(
                patient.Id, facilityId, departmentId, null, clinicId, null, scheduledUtc, chiefComplaint, "Đặt từ Cổng bệnh nhân"), cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.PortalAccess)]
        [HttpPost]
        public async Task<IActionResult> UpdateContact(string? phoneNumber, string? email, string? address, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PortalUserId == userId && !p.IsMerged, cancellationToken);
            if (patient is null)
            {
                TempData["Error"] = "Tài khoản chưa liên kết hồ sơ bệnh nhân.";
                return RedirectToAction(nameof(Index));
            }
            var res = await patientService.UpdatePortalContactAsync(patient.Id, phoneNumber, email, address, cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(PermissionCodes.PortalAccess)]
        [HttpPost]
        public async Task<IActionResult> CancelAppointment(Guid appointmentId, string? reason, CancellationToken cancellationToken)
        {
            var userId = currentUser.UserId ?? Guid.Empty;
            var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.PortalUserId == userId && !p.IsMerged, cancellationToken);
            if (patient is null) return Forbid();
            var ownsAppt = await db.Appointments.AnyAsync(a => a.Id == appointmentId && a.PatientId == patient.Id, cancellationToken);
            if (!ownsAppt) return Forbid();

            var res = await receptionService.UpdateAppointmentStatusAsync(appointmentId, AppointmentStatus.Cancelled, reason ?? "Bệnh nhân hủy qua cổng thông tin", cancellationToken);
            TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
            return RedirectToAction(nameof(Index));
        }
    }
}

namespace SmartHospital.Web.Areas.Reports.Controllers
{
    [Area("Reports")]
    [Authorize]
    public class ReportsController(
        IHospitalDbContext db,
        IMasterDataService masterDataService,
        ICurrentUserService currentUser) : Controller
    {
        [HasPermission(PermissionCodes.ReportRead)]
        public async Task<IActionResult> Index(DateOnly? fromDate, DateOnly? toDate, Guid? facilityId, CancellationToken cancellationToken)
        {
            var facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
            var facId = facilityId ?? currentUser.ActiveFacilityId ?? facilities.FirstOrDefault()?.Id ?? Guid.Empty;
            var today = HospitalTime.TodayVietnam();
            var from = fromDate ?? today.AddDays(-7);
            var to = toDate ?? today;

            var startUtc = new DateTimeOffset(from.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            var endUtc = new DateTimeOffset(to.ToDateTime(TimeOnly.MaxValue), TimeSpan.FromHours(7)).ToUniversalTime();

            ViewBag.Facilities = facilities;
            ViewBag.SelectedFacilityId = facId;
            ViewBag.FromDate = from;
            ViewBag.ToDate = to;

            ViewBag.EncounterCount = await db.Encounters.CountAsync(e => e.FacilityId == facId && e.StartedAtUtc >= startUtc && e.StartedAtUtc <= endUtc, cancellationToken);
            ViewBag.CompletedEncounterCount = await db.Encounters.CountAsync(e => e.FacilityId == facId && e.Status == EncounterStatus.Completed && e.StartedAtUtc >= startUtc && e.StartedAtUtc <= endUtc, cancellationToken);
            ViewBag.AdmissionCount = await db.Admissions.CountAsync(a => a.FacilityId == facId && a.AdmittedAtUtc >= startUtc && a.AdmittedAtUtc <= endUtc, cancellationToken);
            ViewBag.LabOrderCount = await db.LabOrders.CountAsync(l => l.FacilityId == facId && l.OrderedAtUtc >= startUtc && l.OrderedAtUtc <= endUtc, cancellationToken);
            ViewBag.TotalInvoiced = await db.Invoices.Where(i => i.FacilityId == facId && i.IssuedAtUtc >= startUtc && i.IssuedAtUtc <= endUtc).SumAsync(i => (decimal?)i.TotalAmount, cancellationToken) ?? 0m;
            ViewBag.TotalCollected = await db.Payments.Where(p => p.FacilityId == facId && p.PaidAtUtc >= startUtc && p.PaidAtUtc <= endUtc).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
            ViewBag.TotalRefunded = await db.Refunds.Where(r => r.RefundedAtUtc >= startUtc && r.RefundedAtUtc <= endUtc).SumAsync(r => (decimal?)r.Amount, cancellationToken) ?? 0m;

            return View();
        }
    }
}
