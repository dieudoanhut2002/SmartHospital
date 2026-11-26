using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Patients;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Patients.Controllers;

[Area("Patients")]
[Authorize]
public class PatientsController(
    IPatientService patientService,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.PatientRead)]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await patientService.SearchPatientsAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, cancellationToken);
        ViewBag.Search = search;
        return View(result);
    }

    [HasPermission(PermissionCodes.PatientCreate)]
    [HttpGet]
    public IActionResult Create() => View();

    [HasPermission(PermissionCodes.PatientCreate)]
    [HttpPost]
    public async Task<IActionResult> Create(
        string fullName, DateOnly? dateOfBirth, Gender gender, string? phoneNumber, string? email,
        string? nationalId, string? address, string? bloodType, bool requiresVerification,
        string? emergencyContactName, string? emergencyContactRelationship, string? emergencyContactPhone,
        bool confirmDuplicateOverride = false, CancellationToken cancellationToken = default)
    {
        if (!confirmDuplicateOverride)
        {
            var duplicates = await patientService.FindDuplicateCandidatesAsync(fullName, dateOfBirth, phoneNumber, nationalId, null, cancellationToken);
            if (duplicates.Count > 0)
            {
                ViewBag.DuplicateCandidates = duplicates;
                ViewBag.FormData = new CreateOrUpdatePatientRequest(
                    fullName, dateOfBirth, gender, phoneNumber, email, nationalId, address, bloodType,
                    requiresVerification, emergencyContactName, emergencyContactRelationship, emergencyContactPhone, false);
                TempData["Error"] = "Phát hiện hồ sơ có thông tin tương tự. Vui lòng kiểm tra danh sách bên dưới hoặc tích chọn xác nhận tạo mới.";
                return View();
            }
        }

        var res = await patientService.CreatePatientAsync(new CreateOrUpdatePatientRequest(
            fullName, dateOfBirth, gender, phoneNumber, email, nationalId, address, bloodType,
            requiresVerification, emergencyContactName, emergencyContactRelationship, emergencyContactPhone, confirmDuplicateOverride),
            cancellationToken);

        if (!res.Succeeded)
        {
            TempData["Error"] = res.Message;
            return View();
        }

        TempData["Success"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = res.Data });
    }

    [HasPermission(PermissionCodes.PatientRead)]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetPatientDetailAsync(id, cancellationToken);
        if (patient is null) return NotFound();

        ViewBag.Encounters = await db.Encounters.AsNoTracking()
            .Where(e => e.PatientId == id)
            .OrderByDescending(e => e.StartedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        ViewBag.Invoices = await db.Invoices.AsNoTracking()
            .Where(i => i.PatientId == id)
            .OrderByDescending(i => i.IssuedAtUtc)
            .Take(20)
            .ToListAsync(cancellationToken);
        ViewBag.InsuranceProfiles = await db.InsuranceProfiles.AsNoTracking()
            .Where(ip => ip.PatientId == id && ip.IsActive)
            .ToListAsync(cancellationToken);
        return View(patient);
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetPatientDetailAsync(id, cancellationToken);
        if (patient is null) return NotFound();
        return View(patient);
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> Edit(
        Guid id, string fullName, DateOnly? dateOfBirth, Gender gender, string? phoneNumber, string? email,
        string? nationalId, string? address, string? bloodType, bool requiresVerification, CancellationToken cancellationToken)
    {
        var res = await patientService.UpdatePatientAsync(id, new CreateOrUpdatePatientRequest(
            fullName, dateOfBirth, gender, phoneNumber, email, nationalId, address, bloodType,
            requiresVerification, null, null, null, true), cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> AddAllergy(Guid patientId, string allergen, AllergySeverity severity, string? reaction, string? notes, CancellationToken cancellationToken)
    {
        var res = await patientService.AddAllergyAsync(patientId, allergen, severity, reaction, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> AddAlert(Guid patientId, string title, string? detail, string severity, CancellationToken cancellationToken)
    {
        var res = await patientService.AddAlertAsync(patientId, title, detail, severity, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> DeleteAllergy(Guid patientId, Guid allergyId, CancellationToken cancellationToken)
    {
        var res = await patientService.DeleteAllergyAsync(allergyId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> DeleteAlert(Guid patientId, Guid alertId, CancellationToken cancellationToken)
    {
        var res = await patientService.DeleteAlertAsync(alertId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> AddEmergencyContact(Guid patientId, string fullName, string relationship, string phoneNumber, string? address, CancellationToken cancellationToken)
    {
        var res = await patientService.AddEmergencyContactAsync(patientId, fullName, relationship, phoneNumber, address, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> DeleteEmergencyContact(Guid patientId, Guid contactId, CancellationToken cancellationToken)
    {
        var res = await patientService.DeleteEmergencyContactAsync(contactId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> AddInsuranceProfile(Guid patientId, string cardNumber, string? providerName, DateOnly? validFrom, DateOnly? validTo, string? registeredFacilityCode, CancellationToken cancellationToken)
    {
        var res = await patientService.AddInsuranceProfileAsync(patientId, cardNumber, providerName, validFrom, validTo, registeredFacilityCode, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientUpdate)]
    [HttpPost]
    public async Task<IActionResult> DeleteInsuranceProfile(Guid patientId, Guid insuranceProfileId, CancellationToken cancellationToken)
    {
        var res = await patientService.DeleteInsuranceProfileAsync(insuranceProfileId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = patientId });
    }

    [HasPermission(PermissionCodes.PatientMerge)]
    [HttpPost]
    public async Task<IActionResult> Merge(Guid sourcePatientId, Guid targetPatientId, string reason, CancellationToken cancellationToken)
    {
        var res = await patientService.MergePatientsAsync(sourcePatientId, targetPatientId, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = res.Succeeded ? targetPatientId : sourcePatientId });
    }
}
