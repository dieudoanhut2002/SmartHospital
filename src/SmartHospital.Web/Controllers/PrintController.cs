using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Billing;
using SmartHospital.Application.Clinical;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Laboratory;

namespace SmartHospital.Web.Controllers;

[Authorize]
public class PrintController(
    IHospitalDbContext db,
    IClinicalService clinicalService,
    ILabAndImagingService labService,
    IBillingService billingService) : Controller
{
    private async Task LoadHospitalHeaderAsync(CancellationToken cancellationToken)
    {
        ViewBag.Hospital = await db.Hospitals.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        ViewBag.Facility = await db.Facilities.AsNoTracking().FirstOrDefaultAsync(f => f.IsDefault, cancellationToken);
    }

    // 1. Phiếu tiếp nhận & 2. Phiếu số thứ tự
    public async Task<IActionResult> QueueTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var ticket = await db.QueueTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == ticketId, cancellationToken);
        if (ticket is null) return NotFound();
        var encounter = await db.Encounters.AsNoTracking().FirstAsync(e => e.Id == ticket.EncounterId, cancellationToken);
        var patient = await db.Patients.AsNoTracking().FirstAsync(p => p.Id == encounter.PatientId, cancellationToken);
        var clinic = await db.Clinics.AsNoTracking().FirstAsync(c => c.Id == ticket.ClinicId, cancellationToken);

        ViewBag.Encounter = encounter;
        ViewBag.Patient = patient;
        ViewBag.Clinic = clinic;
        return View(ticket);
    }

    // 3. Phiếu khám & 4. Phiếu chỉ định
    public async Task<IActionResult> ClinicalSummary(Guid encounterId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var ws = await clinicalService.GetEncounterWorkspaceAsync(encounterId, cancellationToken);
        if (ws is null) return NotFound();
        return View(ws);
    }

    // 5. Phiếu kết quả xét nghiệm
    public async Task<IActionResult> LabResult(Guid labOrderId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var order = await labService.GetLabOrderDetailAsync(labOrderId, cancellationToken);
        if (order is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == order.PatientId, cancellationToken);
        return View(order);
    }

    // 6. Đơn thuốc
    public async Task<IActionResult> Prescription(Guid prescriptionId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var rx = await db.Prescriptions.AsNoTracking().Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);
        if (rx is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == rx.PatientId, cancellationToken);
        return View(rx);
    }

    // 7. Bảng kê chi phí & Phiếu thu
    public async Task<IActionResult> Invoice(Guid invoiceId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var invoice = await billingService.GetInvoiceDetailAsync(invoiceId, cancellationToken);
        if (invoice is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == invoice.PatientId, cancellationToken);
        return View(invoice);
    }

    // 8. Tóm tắt ra viện
    public async Task<IActionResult> DischargeSummary(Guid admissionId, CancellationToken cancellationToken)
    {
        await LoadHospitalHeaderAsync(cancellationToken);
        var admission = await db.Admissions.AsNoTracking().FirstOrDefaultAsync(a => a.Id == admissionId, cancellationToken);
        if (admission is null) return NotFound();
        ViewBag.Patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == admission.PatientId, cancellationToken);
        ViewBag.Department = await db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == admission.DepartmentId, cancellationToken);
        return View(admission);
    }
}
