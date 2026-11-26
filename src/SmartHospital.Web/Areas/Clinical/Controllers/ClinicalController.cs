using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Clinical;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.MasterData;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Clinical.Controllers;

[Area("Clinical")]
[Authorize]
public class ClinicalController(
    IClinicalService clinicalService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.ClinicalRead)]
    public async Task<IActionResult> Index(string? search, EncounterStatus? status, Guid? clinicId, int page = 1, CancellationToken cancellationToken = default)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id;
        var result = await clinicalService.GetWorklistAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, clinicId, status, cancellationToken);
        ViewBag.Search = search;
        ViewBag.Status = status;
        ViewBag.Clinics = await masterDataService.GetClinicsAsync(facId, cancellationToken);
        return View(result);
    }

    [HasPermission(PermissionCodes.ClinicalRead)]
    public async Task<IActionResult> Workspace(Guid id, CancellationToken cancellationToken)
    {
        var ws = await clinicalService.GetEncounterWorkspaceAsync(id, cancellationToken);
        if (ws is null) return NotFound();

        ViewBag.Services = await masterDataService.GetServicesAsync(cancellationToken);
        ViewBag.LabTests = await masterDataService.GetLabTestsAsync(cancellationToken);
        ViewBag.ImagingServices = await masterDataService.GetImagingServicesAsync(cancellationToken);
        ViewBag.Medicines = await masterDataService.GetMedicinesAsync(cancellationToken);
        ViewBag.IcdDiagnoses = await db.Diagnoses.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.IcdCode).ToListAsync(cancellationToken);
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(ws.Encounter.FacilityId, cancellationToken);
        return View(ws);
    }

    [HasPermission(PermissionCodes.ClinicalWrite)]
    [HttpPost]
    public async Task<IActionResult> RecordVitalSign(
        Guid encounterId, int? systolicBp, int? diastolicBp, int? heartRate,
        decimal? temperatureC, int? respiratoryRate, int? spO2, decimal? heightCm, decimal? weightKg, string? notes,
        CancellationToken cancellationToken)
    {
        var res = await clinicalService.RecordVitalSignAsync(new RecordVitalSignRequest(
            encounterId, systolicBp, diastolicBp, heartRate, temperatureC, respiratoryRate, spO2, heightCm, weightKg, notes), cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalWrite)]
    [HttpPost]
    public async Task<IActionResult> SaveNoteDraft(
        Guid encounterId, string? chiefComplaint, string? historyOfPresentIllness, string? pastMedicalHistory,
        string? physicalExam, string? preliminaryDiagnosis, string? finalDiagnosis, string? treatmentPlan,
        string? patientInstructions, DateOnly? followUpDate, CancellationToken cancellationToken)
    {
        var res = await clinicalService.SaveClinicalNoteDraftAsync(new SaveClinicalNoteRequest(
            encounterId, chiefComplaint, historyOfPresentIllness, pastMedicalHistory, physicalExam,
            preliminaryDiagnosis, finalDiagnosis, treatmentPlan, patientInstructions, followUpDate), cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalNoteSign)]
    [HttpPost]
    public async Task<IActionResult> SignNote(Guid encounterId, CancellationToken cancellationToken)
    {
        var res = await clinicalService.SignClinicalNoteAsync(encounterId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalNoteAmend)]
    [HttpPost]
    public async Task<IActionResult> AmendNote(
        Guid encounterId, string? chiefComplaint, string? historyOfPresentIllness, string? pastMedicalHistory,
        string? physicalExam, string? preliminaryDiagnosis, string? finalDiagnosis, string? treatmentPlan,
        string? patientInstructions, DateOnly? followUpDate, string amendmentReason, CancellationToken cancellationToken)
    {
        var res = await clinicalService.AmendClinicalNoteAsync(new SaveClinicalNoteRequest(
            encounterId, chiefComplaint, historyOfPresentIllness, pastMedicalHistory, physicalExam,
            preliminaryDiagnosis, finalDiagnosis, treatmentPlan, patientInstructions, followUpDate), amendmentReason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalWrite)]
    [HttpPost]
    public async Task<IActionResult> AddDiagnosis(Guid encounterId, string icdCode, string diagnosisName, bool isPrimary, string diagnosisType, CancellationToken cancellationToken)
    {
        var res = await clinicalService.AddDiagnosisAsync(encounterId, icdCode, diagnosisName, isPrimary, diagnosisType, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalOrderCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateServiceOrder(Guid encounterId, Guid serviceCatalogId, int quantity, string? notes, CancellationToken cancellationToken)
    {
        var res = await clinicalService.CreateServiceOrderAsync(encounterId, serviceCatalogId, quantity, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalOrderCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateLabOrder(Guid encounterId, Guid[] labTestIds, string? clinicalIndication, CancellationToken cancellationToken)
    {
        var res = await clinicalService.CreateLabOrderAsync(encounterId, labTestIds ?? [], clinicalIndication, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalOrderCreate)]
    [HttpPost]
    public async Task<IActionResult> CreateImagingOrder(Guid encounterId, Guid imagingServiceId, string? clinicalIndication, CancellationToken cancellationToken)
    {
        var res = await clinicalService.CreateImagingOrderAsync(encounterId, imagingServiceId, clinicalIndication, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.PrescriptionCreate)]
    [HttpPost]
    public async Task<IActionResult> CreatePrescription(
        Guid encounterId, Guid[] medicineId, string[] dosageInstruction, string[] route, int[] durationDays, decimal[] quantityPrescribed,
        string? diagnosisSummary, string? notes, CancellationToken cancellationToken)
    {
        var items = new List<CreatePrescriptionItemInput>();
        for (int i = 0; i < (medicineId?.Length ?? 0); i++)
        {
            if (medicineId![i] == Guid.Empty) continue;
            items.Add(new CreatePrescriptionItemInput(
                medicineId[i],
                dosageInstruction.ElementAtOrDefault(i) ?? "Theo chỉ dẫn bác sĩ",
                route.ElementAtOrDefault(i),
                durationDays.ElementAtOrDefault(i) > 0 ? durationDays[i] : 5,
                quantityPrescribed.ElementAtOrDefault(i) > 0 ? quantityPrescribed[i] : 1));
        }

        var res = await clinicalService.CreatePrescriptionAsync(encounterId, items, diagnosisSummary, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalWrite)]
    [HttpPost]
    public async Task<IActionResult> CompleteEncounter(Guid encounterId, CancellationToken cancellationToken)
    {
        var res = await clinicalService.CompleteEncounterAsync(encounterId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }

    [HasPermission(PermissionCodes.ClinicalWrite)]
    [HttpPost]
    public async Task<IActionResult> RequestAdmission(Guid encounterId, Guid targetDepartmentId, string reason, string? preliminaryDiagnosis, CancellationToken cancellationToken)
    {
        var res = await clinicalService.RequestAdmissionAsync(encounterId, targetDepartmentId, reason, preliminaryDiagnosis, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Workspace), new { id = encounterId });
    }
}
