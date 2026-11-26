using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Reception;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Reception.Controllers;

[Area("Reception")]
[Authorize]
public class ReceptionController(
    IReceptionService receptionService,
    IMasterDataService masterDataService,
    IPatientService patientService,
    ICurrentUserService currentUser) : Controller
{
    [HasPermission(PermissionCodes.AppointmentRead)]
    public async Task<IActionResult> Appointments(string? search, AppointmentStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
        var facId = currentUser.ActiveFacilityId ?? facilities.FirstOrDefault()?.Id;
        var result = await receptionService.GetAppointmentsAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, status, null, cancellationToken);

        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(facId, cancellationToken);
        ViewBag.Clinics = await masterDataService.GetClinicsAsync(facId, cancellationToken);
        ViewBag.Facilities = facilities;
        ViewBag.ActiveFacilityId = facId;
        ViewBag.Patients = (await patientService.SearchPatientsAsync(new PagedRequest { PageSize = 50 }, cancellationToken)).Items;
        return View(result);
    }

    [HasPermission(PermissionCodes.AppointmentManage)]
    [HttpPost]
    public async Task<IActionResult> CreateAppointment(
        Guid patientId, Guid facilityId, Guid departmentId, Guid? clinicId,
        DateTime scheduledLocal, string? chiefComplaint, string? notes, CancellationToken cancellationToken)
    {
        var vnOffset = TimeSpan.FromHours(7);
        var scheduledUtc = new DateTimeOffset(DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Unspecified), vnOffset).ToUniversalTime();
        var res = await receptionService.CreateAppointmentAsync(new CreateAppointmentRequest(
            patientId, facilityId, departmentId, null, clinicId, null, scheduledUtc, chiefComplaint, notes), cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Appointments));
    }

    [HasPermission(PermissionCodes.AppointmentManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateAppointmentStatus(Guid appointmentId, AppointmentStatus newStatus, string? reason, CancellationToken cancellationToken)
    {
        var res = await receptionService.UpdateAppointmentStatusAsync(appointmentId, newStatus, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Appointments));
    }

    [HasPermission(PermissionCodes.AppointmentManage)]
    [HttpPost]
    public async Task<IActionResult> RescheduleAppointment(Guid appointmentId, DateTime scheduledLocal, Guid? clinicId, string? notes, CancellationToken cancellationToken)
    {
        var scheduledUtc = new DateTimeOffset(DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToUniversalTime();
        var res = await receptionService.RescheduleAppointmentAsync(appointmentId, scheduledUtc, clinicId, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Appointments));
    }

    [HasPermission(PermissionCodes.AppointmentRead)]
    public async Task<IActionResult> QueueBoard(Guid? clinicId = null, Guid? patientId = null, CancellationToken cancellationToken = default)
    {
        var facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
        var facId = currentUser.ActiveFacilityId ?? facilities.FirstOrDefault()?.Id ?? Guid.Empty;

        var board = await receptionService.GetQueueBoardAsync(facId, clinicId, null, cancellationToken);
        ViewBag.Clinics = await masterDataService.GetClinicsAsync(facId, cancellationToken);
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(facId, cancellationToken);
        ViewBag.ActiveFacilityId = facId;
        ViewBag.SelectedClinicId = clinicId;
        ViewBag.SelectedPatientId = patientId;
        ViewBag.Patients = (await patientService.SearchPatientsAsync(new PagedRequest { PageSize = 50 }, cancellationToken)).Items;
        return View(board);
    }

    [HasPermission(PermissionCodes.EncounterStart)]
    [HttpPost]
    public async Task<IActionResult> WalkInCheckIn(
        Guid patientId, Guid facilityId, Guid departmentId, Guid clinicId,
        Guid? appointmentId, string? chiefComplaint, int priority = 0, CancellationToken cancellationToken = default)
    {
        var res = await receptionService.CreateEncounterWithQueueAsync(new WalkInOrCheckInRequest(
            patientId, facilityId, departmentId, clinicId, null, null, appointmentId,
            EncounterType.Outpatient, chiefComplaint, priority), cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(QueueBoard), new { clinicId });
    }

    [HasPermission(PermissionCodes.QueueManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateTicketStatus(Guid ticketId, QueueTicketStatus newStatus, Guid? transferToClinicId, string? notes, CancellationToken cancellationToken)
    {
        var res = await receptionService.UpdateQueueTicketStatusAsync(ticketId, newStatus, transferToClinicId, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(QueueBoard));
    }
}
