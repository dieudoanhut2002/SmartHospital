using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Billing;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Billing.Controllers;

[Area("Billing")]
[Authorize]
public class BillingController(
    IBillingService billingService,
    IMasterDataService masterDataService,
    IPatientService patientService,
    ICurrentUserService currentUser,
    IHospitalDbContext db) : Controller
{
    [HasPermission(PermissionCodes.BillingRead)]
    public async Task<IActionResult> Index(string? search, InvoiceStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        var invoices = await billingService.GetInvoicesAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, status, cancellationToken);
        ViewBag.Search = search;
        ViewBag.ActiveFacilityId = facId;
        ViewBag.Patients = (await patientService.SearchPatientsAsync(new PagedRequest { PageSize = 50 }, cancellationToken)).Items;
        ViewBag.CashierReport = await billingService.GetCashierDailyReportAsync(facId, HospitalTime.TodayVietnam(), cancellationToken);
        return View(invoices);
    }

    [HasPermission(PermissionCodes.BillingChargeManage)]
    [HttpGet]
    public async Task<IActionResult> CreateInvoice(Guid patientId, Guid? encounterId, CancellationToken cancellationToken)
    {
        var preview = await billingService.PreviewUnbilledChargesAsync(patientId, encounterId, cancellationToken);
        if (preview is null) return NotFound();
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.ActiveFacilityId = facId;
        ViewBag.Encounters = await db.Encounters.AsNoTracking().Where(e => e.PatientId == patientId).OrderByDescending(e => e.StartedAtUtc).Take(10).ToListAsync(cancellationToken);
        return View(preview);
    }

    [HasPermission(PermissionCodes.BillingChargeManage)]
    [HttpPost]
    public async Task<IActionResult> CreateInvoice(Guid patientId, Guid facilityId, Guid? encounterId, decimal insuranceCoveredAmount, string? notes, CancellationToken cancellationToken)
    {
        var res = await billingService.CreateInvoiceFromUnbilledChargesAsync(patientId, facilityId, encounterId, insuranceCoveredAmount, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        if (res.Succeeded) return RedirectToAction(nameof(Detail), new { id = res.Data });
        return RedirectToAction(nameof(CreateInvoice), new { patientId, encounterId });
    }

    [HasPermission(PermissionCodes.BillingRead)]
    public async Task<IActionResult> Detail(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await billingService.GetInvoiceDetailAsync(id, cancellationToken);
        if (invoice is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == invoice.PatientId, cancellationToken);
        ViewBag.Adjustments = await db.BillingAdjustments.AsNoTracking().Where(a => a.InvoiceId == id).OrderByDescending(a => a.AdjustedAtUtc).ToListAsync(cancellationToken);
        ViewBag.Claims = await db.InsuranceClaims.AsNoTracking().Where(c => c.InvoiceId == id).ToListAsync(cancellationToken);
        return View(invoice);
    }

    [HasPermission(PermissionCodes.BillingPaymentCollect)]
    [HttpPost]
    public async Task<IActionResult> CollectPayment(Guid invoiceId, decimal amount, PaymentMethod paymentMethod, bool isAdvance, string? notes, CancellationToken cancellationToken)
    {
        var res = await billingService.RecordPaymentAsync(invoiceId, amount, paymentMethod, isAdvance, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = invoiceId });
    }

    [HasPermission(PermissionCodes.BillingRefundApprove)]
    [HttpPost]
    public async Task<IActionResult> ProcessRefund(Guid invoiceId, Guid paymentId, decimal refundAmount, string reason, CancellationToken cancellationToken)
    {
        var res = await billingService.ProcessRefundAsync(paymentId, refundAmount, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = invoiceId });
    }

    [HasPermission(PermissionCodes.BillingRefundApprove)]
    [HttpPost]
    public async Task<IActionResult> ApplyAdjustment(Guid invoiceId, decimal adjustmentAmount, string reason, CancellationToken cancellationToken)
    {
        var res = await billingService.ApplyAdjustmentAsync(invoiceId, adjustmentAmount, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = invoiceId });
    }

    [HasPermission(PermissionCodes.BillingPaymentCollect)]
    [HttpPost]
    public async Task<IActionResult> IssueDraftEInvoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        var res = await billingService.IssueDraftEInvoiceDemoAsync(invoiceId, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = invoiceId });
    }

    [HasPermission(PermissionCodes.BillingChargeManage)]
    [HttpPost]
    public async Task<IActionResult> SubmitInsuranceStaging(Guid invoiceId, decimal claimedAmount, string? notes, CancellationToken cancellationToken)
    {
        var res = await billingService.CreateInsuranceClaimStagingAsync(invoiceId, null, claimedAmount, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Detail), new { id = invoiceId });
    }
}
