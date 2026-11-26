using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Inpatient.Controllers;

[Area("Inpatient")]
[Authorize]
public class InpatientController(
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.InpatientRead)]
    public async Task<IActionResult> Index(Guid? departmentId, CancellationToken cancellationToken)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.BedBoard = await opsService.GetBedBoardAsync(facId, departmentId, cancellationToken);
        ViewBag.Admissions = await opsService.GetAdmissionsAsync(facId, AdmissionStatus.Active, cancellationToken);
        ViewBag.PendingRequests = await opsService.GetPendingAdmissionRequestsAsync(facId, cancellationToken);
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(facId, cancellationToken);
        ViewBag.ActiveFacilityId = facId;
        return View();
    }

    [HasPermission(PermissionCodes.InpatientManage)]
    [HttpPost]
    public async Task<IActionResult> AdmitFromRequest(Guid admissionRequestId, Guid bedId, string? admissionDiagnosis, CancellationToken cancellationToken)
    {
        var req = await db.AdmissionRequests.AsNoTracking().FirstOrDefaultAsync(r => r.Id == admissionRequestId, cancellationToken);
        if (req is null) return NotFound();

        var bed = await db.Beds.AsNoTracking().FirstOrDefaultAsync(b => b.Id == bedId, cancellationToken);
        if (bed is null)
        {
            TempData["Error"] = "Không tìm thấy giường.";
            return RedirectToAction(nameof(Index));
        }

        var res = await opsService.AdmitPatientAsync(req.PatientId, req.EncounterId, req.FacilityId, bed.DepartmentId, bedId, admissionDiagnosis ?? req.PreliminaryDiagnosis, req.Id, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.InpatientManage)]
    [HttpPost]
    public async Task<IActionResult> TransferBed(Guid admissionId, Guid newBedId, string reason, CancellationToken cancellationToken)
    {
        var res = await opsService.TransferBedAsync(admissionId, newBedId, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.InpatientManage)]
    [HttpPost]
    public async Task<IActionResult> Discharge(Guid admissionId, string dischargeSummary, string? dischargeInstructions, DateOnly? followUpDate, CancellationToken cancellationToken)
    {
        var res = await opsService.DischargePatientAsync(admissionId, dischargeSummary, dischargeInstructions, followUpDate, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.InpatientManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateBedStatus(Guid bedId, BedStatus newStatus, string? reason, CancellationToken cancellationToken)
    {
        var res = await opsService.UpdateBedStatusAsync(bedId, newStatus, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}

[Area("Inpatient")]
[Authorize]
public class NursingController(
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser) : Controller
{
    [HasPermission(PermissionCodes.NursingRead)]
    public async Task<IActionResult> Index(Guid? admissionId, CancellationToken cancellationToken)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        var admissions = await opsService.GetAdmissionsAsync(facId, AdmissionStatus.Active, cancellationToken);
        var selectedId = admissionId ?? admissions.FirstOrDefault()?.AdmissionId;

        ViewBag.Admissions = admissions;
        ViewBag.SelectedAdmissionId = selectedId;
        if (selectedId.HasValue)
        {
            ViewBag.Notes = await opsService.GetNursingNotesAsync(selectedId.Value, cancellationToken);
            ViewBag.Observations = await opsService.GetNursingObservationsAsync(selectedId.Value, cancellationToken);
            ViewBag.MedAdmins = await opsService.GetMedicationAdministrationsAsync(selectedId.Value, cancellationToken);
        }
        return View();
    }

    [HasPermission(PermissionCodes.NursingWrite)]
    [HttpPost]
    public async Task<IActionResult> AddNote(Guid admissionId, string noteText, CancellationToken cancellationToken)
    {
        var res = await opsService.AddNursingNoteAsync(admissionId, noteText, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index), new { admissionId });
    }

    [HasPermission(PermissionCodes.NursingWrite)]
    [HttpPost]
    public async Task<IActionResult> AddObservation(Guid admissionId, int? systolicBp, int? diastolicBp, int? heartRate, decimal? temperatureC, int? spO2, string? fluidIo, CancellationToken cancellationToken)
    {
        var res = await opsService.RecordNursingObservationAsync(admissionId, systolicBp, diastolicBp, heartRate, temperatureC, spO2, fluidIo, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index), new { admissionId });
    }

    [HasPermission(PermissionCodes.NursingWrite)]
    [HttpPost]
    public async Task<IActionResult> RecordMedication(Guid admissionId, string medicineName, string dose, string status, string? omissionReason, CancellationToken cancellationToken)
    {
        var res = await opsService.RecordMedicationAdministrationAsync(admissionId, medicineName, dose, status, omissionReason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index), new { admissionId });
    }
}

[Area("Inpatient")]
[Authorize]
public class EmergencyController(
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    IPatientService patientService,
    ICurrentUserService currentUser) : Controller
{
    [HasPermission(PermissionCodes.EmergencyManage)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.Intakes = await opsService.GetEmergencyIntakesAsync(facId, cancellationToken);
        ViewBag.Patients = (await patientService.SearchPatientsAsync(new PagedRequest { PageSize = 50 }, cancellationToken)).Items;
        ViewBag.ActiveFacilityId = facId;
        return View();
    }

    [HasPermission(PermissionCodes.EmergencyManage)]
    [HttpPost]
    public async Task<IActionResult> CreateIntake(Guid patientId, Guid facilityId, EmergencyTriageLevel triageLevel, string initialCondition, string? bayLocation, CancellationToken cancellationToken)
    {
        var res = await opsService.CreateEmergencyIntakeAsync(patientId, facilityId, triageLevel, initialCondition, bayLocation, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}

[Area("Inpatient")]
[Authorize]
public class SurgeryController(
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    IPatientService patientService,
    ICurrentUserService currentUser,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.SurgeryRead)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.Requests = await opsService.GetSurgeryRequestsAsync(facId, cancellationToken);
        ViewBag.Rooms = (await masterDataService.GetRoomsAsync(facId, cancellationToken)).Where(r => r.RoomType == RoomType.OperatingRoom).ToList();
        ViewBag.Patients = (await patientService.SearchPatientsAsync(new PagedRequest { PageSize = 50 }, cancellationToken)).Items;
        ViewBag.Encounters = await db.Encounters.AsNoTracking().OrderByDescending(e => e.StartedAtUtc).Take(30).ToListAsync(cancellationToken);
        ViewBag.ActiveFacilityId = facId;
        return View();
    }

    [HasPermission(PermissionCodes.SurgeryManage)]
    [HttpPost]
    public async Task<IActionResult> CreateRequest(Guid patientId, Guid encounterId, Guid facilityId, string procedureName, DateTime? scheduledLocal, Guid? operatingRoomId, string? teamSummary, string? checklistSummary, CancellationToken cancellationToken)
    {
        DateTimeOffset? scheduledUtc = scheduledLocal.HasValue
            ? new DateTimeOffset(DateTime.SpecifyKind(scheduledLocal.Value, DateTimeKind.Unspecified), TimeSpan.FromHours(7)).ToUniversalTime()
            : null;
        var res = await opsService.CreateSurgeryRequestAsync(patientId, encounterId, facilityId, procedureName, scheduledUtc, operatingRoomId, teamSummary, checklistSummary, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.SurgeryManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(Guid surgeryId, SurgeryStatus status, string? operativeNote, string? suppliesUsed, CancellationToken cancellationToken)
    {
        var res = await opsService.UpdateSurgeryStatusAsync(surgeryId, status, operativeNote, suppliesUsed, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}
