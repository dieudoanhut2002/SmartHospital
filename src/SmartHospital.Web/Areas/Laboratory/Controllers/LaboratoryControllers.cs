using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Laboratory;
using SmartHospital.Application.MasterData;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Laboratory.Controllers;

[Area("Laboratory")]
[Authorize]
public class LaboratoryController(
    ILabAndImagingService labService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.LabRead)]
    public async Task<IActionResult> Index(string? search, LabOrderStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id;
        var result = await labService.GetLabWorklistAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, status, cancellationToken);
        ViewBag.Search = search;
        ViewBag.Status = status;
        return View(result);
    }

    [HasPermission(PermissionCodes.LabRead)]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var order = await labService.GetLabOrderDetailAsync(id, cancellationToken);
        if (order is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == order.PatientId, cancellationToken);
        return View(order);
    }

    [HasPermission(PermissionCodes.LabSpecimenManage)]
    [HttpPost]
    public async Task<IActionResult> CollectSpecimen(Guid labOrderId, string? storageLocation, CancellationToken cancellationToken)
    {
        var res = await labService.CollectSpecimenAsync(labOrderId, storageLocation, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }

    [HasPermission(PermissionCodes.LabSpecimenManage)]
    [HttpPost]
    public async Task<IActionResult> ReceiveSpecimen(Guid labOrderId, CancellationToken cancellationToken)
    {
        var res = await labService.ReceiveSpecimenAsync(labOrderId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }

    [HasPermission(PermissionCodes.LabSpecimenManage)]
    [HttpPost]
    public async Task<IActionResult> RejectSpecimen(Guid labOrderId, string reason, CancellationToken cancellationToken)
    {
        var res = await labService.RejectSpecimenAsync(labOrderId, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }

    [HasPermission(PermissionCodes.LabResultEnter)]
    [HttpPost]
    public async Task<IActionResult> EnterResult(Guid labOrderId, Guid labOrderItemId, string resultValue, decimal? numericValue, string? notes, CancellationToken cancellationToken)
    {
        var res = await labService.EnterLabResultAsync(labOrderItemId, resultValue, numericValue, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }

    [HasPermission(PermissionCodes.LabResultVerify)]
    [HttpPost]
    public async Task<IActionResult> VerifyResult(Guid labOrderId, Guid labResultId, CancellationToken cancellationToken)
    {
        var res = await labService.VerifyLabResultAsync(labResultId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }

    [HasPermission(PermissionCodes.LabResultAmend)]
    [HttpPost]
    public async Task<IActionResult> AmendResult(Guid labOrderId, Guid labResultId, string newResultValue, decimal? newNumericValue, string reason, CancellationToken cancellationToken)
    {
        var res = await labService.AmendLabResultAsync(labResultId, newResultValue, newNumericValue, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = labOrderId });
    }
}

[Area("Laboratory")]
[Authorize]
public class ImagingController(
    ILabAndImagingService labService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser,
    IDocumentStorage documentStorage,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.ImagingRead)]
    public async Task<IActionResult> Index(string? search, ImagingOrderStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id;
        var result = await labService.GetImagingWorklistAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, status, cancellationToken);
        ViewBag.Search = search;
        return View(result);
    }

    [HasPermission(PermissionCodes.ImagingRead)]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var order = await labService.GetImagingOrderDetailAsync(id, cancellationToken);
        if (order is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == order.PatientId, cancellationToken);
        ViewBag.Documents = await db.Documents.AsNoTracking().Where(d => d.EntityType == "ImagingOrder" && d.EntityId == id).ToListAsync(cancellationToken);
        return View(order);
    }

    [HasPermission(PermissionCodes.ImagingPerform)]
    [HttpPost]
    public async Task<IActionResult> SaveReport(Guid imagingOrderId, string findings, string conclusion, bool verify = false, CancellationToken cancellationToken = default)
    {
        if (verify && !currentUser.HasPermission(PermissionCodes.ImagingReportVerify))
            return Forbid();

        var res = await labService.SaveImagingReportAsync(imagingOrderId, findings, conclusion, verify, null, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = imagingOrderId });
    }

    [HasPermission(PermissionCodes.ImagingPerform)]
    [HttpPost]
    public async Task<IActionResult> UploadAttachment(Guid imagingOrderId, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            TempData["Error"] = "Vui lòng chọn tệp tải lên.";
            return RedirectToAction(nameof(Detail), new { id = imagingOrderId });
        }

        await using var stream = file.OpenReadStream();
        var res = await labService.UploadImagingAttachmentAsync(imagingOrderId, stream, file.FileName, file.ContentType, file.Length, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = imagingOrderId });
    }

    [HasPermission(PermissionCodes.ImagingRead)]
    public async Task<IActionResult> DownloadDocument(Guid docId, CancellationToken cancellationToken)
    {
        var doc = await db.Documents.AsNoTracking().FirstOrDefaultAsync(d => d.Id == docId, cancellationToken);
        if (doc is null) return NotFound();
        var stream = await documentStorage.OpenReadAsync(doc.StorageKey, cancellationToken);
        if (stream is null) return NotFound();
        return File(stream, doc.ContentType, doc.OriginalFileName);
    }
}
