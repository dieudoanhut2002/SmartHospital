using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Common;

namespace SmartHospital.Application.Billing;

public record InvoiceSummaryDto(
    Guid InvoiceId,
    string InvoiceCode,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    Guid? EncounterId,
    DateTimeOffset IssuedAtUtc,
    InvoiceStatus Status,
    decimal TotalAmount,
    decimal InsuranceCoveredAmount,
    decimal PatientResponsibilityAmount,
    decimal PaidAmount,
    decimal RefundedAmount,
    decimal RemainingBalance);

public record UnbilledChargesPreviewDto(
    Guid PatientId,
    string PatientCode,
    string PatientName,
    Guid? EncounterId,
    IReadOnlyList<UnbilledChargeLine> Lines,
    decimal TotalAmount);

public record UnbilledChargeLine(
    string SourceType,
    Guid SourceId,
    string ItemCode,
    string Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

public interface IBillingService
{
    Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(PagedRequest request, Guid? facilityId = null, InvoiceStatus? status = null, CancellationToken cancellationToken = default);
    Task<Invoice?> GetInvoiceDetailAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<UnbilledChargesPreviewDto?> PreviewUnbilledChargesAsync(Guid patientId, Guid? encounterId = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateInvoiceFromUnbilledChargesAsync(Guid patientId, Guid facilityId, Guid? encounterId, decimal insuranceCoveredAmount = 0, string? notes = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> RecordPaymentAsync(Guid invoiceId, decimal amount, PaymentMethod method, bool isAdvance = false, string? notes = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> ProcessRefundAsync(Guid paymentId, decimal refundAmount, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult> ApplyAdjustmentAsync(Guid invoiceId, decimal adjustmentAmount, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult<string>> IssueDraftEInvoiceDemoAsync(Guid invoiceId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateInsuranceClaimStagingAsync(Guid invoiceId, Guid? insuranceProfileId, decimal claimedAmount, string? notes, CancellationToken cancellationToken = default);
    Task<CashierDailyReportDto> GetCashierDailyReportAsync(Guid facilityId, DateOnly date, CancellationToken cancellationToken = default);
}

public record CashierDailyReportDto(
    DateOnly Date,
    int TotalPaymentsCount,
    decimal TotalCollected,
    decimal TotalCash,
    decimal TotalTransfer,
    decimal TotalCard,
    decimal TotalRefunded,
    decimal NetRevenue);

public class BillingService(
    IHospitalDbContext db,
    ICodeGeneratorService codeGenerator,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    IEInvoiceIntegration eInvoiceIntegration,
    IPaymentGateway paymentGateway,
    IInsuranceIntegration insuranceIntegration) : IBillingService
{
    public async Task<PagedResult<InvoiceSummaryDto>> GetInvoicesAsync(PagedRequest request, Guid? facilityId = null, InvoiceStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = from i in db.Invoices.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on i.PatientId equals p.Id
                    select new { i, p };

        if (facilityId.HasValue) query = query.Where(x => x.i.FacilityId == facilityId.Value);
        if (status.HasValue) query = query.Where(x => x.i.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.i.InvoiceCode.Contains(term) || x.p.PatientCode.Contains(term) || x.p.FullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.i.IssuedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new InvoiceSummaryDto(
                x.i.Id, x.i.InvoiceCode, x.p.Id, x.p.PatientCode, x.p.FullName, x.i.EncounterId,
                x.i.IssuedAtUtc, x.i.Status, x.i.TotalAmount, x.i.InsuranceCoveredAmount,
                x.i.PatientResponsibilityAmount, x.i.PaidAmount, x.i.RefundedAmount,
                Math.Max(0, x.i.PatientResponsibilityAmount - x.i.PaidAmount)))
            .ToListAsync(cancellationToken);

        return new PagedResult<InvoiceSummaryDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<Invoice?> GetInvoiceDetailAsync(Guid invoiceId, CancellationToken cancellationToken = default)
        => await db.Invoices.AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Allocations)
            .ThenInclude(a => a.Payment!)
            .ThenInclude(p => p.Refunds)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

    public async Task<UnbilledChargesPreviewDto?> PreviewUnbilledChargesAsync(Guid patientId, Guid? encounterId = null, CancellationToken cancellationToken = default)
    {
        var patient = await db.Patients.AsNoTracking().FirstOrDefaultAsync(p => p.Id == patientId, cancellationToken);
        if (patient is null) return null;

        var lines = new List<UnbilledChargeLine>();

        // Consultation fee if encounter exists and hasn't been billed yet
        if (encounterId.HasValue)
        {
            var alreadyBilledConsult = await db.InvoiceItems.AnyAsync(
                ii => ii.SourceType == "Consultation" && ii.SourceId == encounterId.Value, cancellationToken);
            if (!alreadyBilledConsult)
            {
                var consultSvc = await db.ServiceCatalogs.AsNoTracking().FirstOrDefaultAsync(s => s.Code == "KB-NOI", cancellationToken);
                var price = consultSvc?.DefaultPrice ?? 150000m;
                lines.Add(new UnbilledChargeLine("Consultation", encounterId.Value, consultSvc?.Code ?? "KB", consultSvc?.Name ?? "Phí khám bệnh", 1, price, price));
            }
        }

        // Service orders
        var svcQuery = db.ServiceOrders.AsNoTracking().Where(s => s.PatientId == patientId && !s.IsBilled && s.Status != OrderStatus.Cancelled);
        if (encounterId.HasValue) svcQuery = svcQuery.Where(s => s.EncounterId == encounterId.Value);
        foreach (var s in await svcQuery.ToListAsync(cancellationToken))
        {
            lines.Add(new UnbilledChargeLine("ServiceOrder", s.Id, s.ServiceCode, s.ServiceName, s.Quantity, s.UnitPriceSnapshot, s.Quantity * s.UnitPriceSnapshot));
        }

        // Lab orders
        var labQuery = db.LabOrders.AsNoTracking().Include(l => l.Items).Where(l => l.PatientId == patientId && !l.IsBilled && l.Status != LabOrderStatus.Cancelled);
        if (encounterId.HasValue) labQuery = labQuery.Where(l => l.EncounterId == encounterId.Value);
        foreach (var l in await labQuery.ToListAsync(cancellationToken))
        {
            var labTotal = l.Items.Sum(i => i.UnitPriceSnapshot);
            lines.Add(new UnbilledChargeLine("LabOrder", l.Id, l.OrderCode, $"Xét nghiệm ({l.Items.Count} chỉ số)", 1, labTotal, labTotal));
        }

        // Imaging orders
        var imgQuery = db.ImagingOrders.AsNoTracking().Where(i => i.PatientId == patientId && !i.IsBilled && i.Status != ImagingOrderStatus.Cancelled);
        if (encounterId.HasValue) imgQuery = imgQuery.Where(i => i.EncounterId == encounterId.Value);
        foreach (var i in await imgQuery.ToListAsync(cancellationToken))
        {
            lines.Add(new UnbilledChargeLine("ImagingOrder", i.Id, i.OrderCode, i.ServiceName, 1, i.UnitPriceSnapshot, i.UnitPriceSnapshot));
        }

        // Dispensed medicines
        var dispQuery = db.PrescriptionDispensings.AsNoTracking().Include(d => d.Items).Where(d => d.PatientId == patientId && !d.IsBilled);
        if (encounterId.HasValue) dispQuery = dispQuery.Where(d => d.EncounterId == encounterId.Value);
        foreach (var d in await dispQuery.ToListAsync(cancellationToken))
        {
            var total = d.Items.Sum(i => i.QuantityDispensed * i.UnitPriceSnapshot);
            lines.Add(new UnbilledChargeLine("PrescriptionDispensing", d.Id, d.DispensingCode, $"Thuốc cấp phát ({d.DispensingCode})", 1, total, total));
        }

        return new UnbilledChargesPreviewDto(patient.Id, patient.PatientCode, patient.FullName, encounterId, lines, lines.Sum(l => l.LineTotal));
    }

    public async Task<OperationResult<Guid>> CreateInvoiceFromUnbilledChargesAsync(
        Guid patientId, Guid facilityId, Guid? encounterId, decimal insuranceCoveredAmount = 0, string? notes = null, CancellationToken cancellationToken = default)
    {
        var preview = await PreviewUnbilledChargesAsync(patientId, encounterId, cancellationToken);
        if (preview is null) return OperationResult<Guid>.Fail("Không tìm thấy bệnh nhân.");
        if (preview.Lines.Count == 0) return OperationResult<Guid>.Fail("Không có khoản phí phát sinh nào chưa lập hóa đơn.");

        if (insuranceCoveredAmount < 0 || insuranceCoveredAmount > preview.TotalAmount)
            return OperationResult<Guid>.Fail("Số tiền BHYT/bên chi trả khác không hợp lệ.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var invoice = new Invoice
        {
            InvoiceCode = await codeGenerator.GenerateNextCodeAsync("HD", "Invoice", cancellationToken),
            PatientId = patientId,
            EncounterId = encounterId,
            FacilityId = facilityId,
            IssuedAtUtc = DateTimeOffset.UtcNow,
            Status = InvoiceStatus.Issued,
            TotalAmount = preview.TotalAmount,
            InsuranceCoveredAmount = insuranceCoveredAmount,
            PatientResponsibilityAmount = preview.TotalAmount - insuranceCoveredAmount,
            Notes = notes?.Trim()
        };

        var ratio = preview.TotalAmount > 0 ? insuranceCoveredAmount / preview.TotalAmount : 0m;
        foreach (var line in preview.Lines)
        {
            var payerAmt = Math.Round(line.LineTotal * ratio, 0);
            invoice.Items.Add(new InvoiceItem
            {
                InvoiceId = invoice.Id,
                SourceType = line.SourceType,
                SourceId = line.SourceId,
                ItemCode = line.ItemCode,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitPriceSnapshot = line.UnitPrice,
                LineTotal = line.LineTotal,
                PayerAmount = payerAmt,
                PatientAmount = line.LineTotal - payerAmt
            });
        }

        // Mark sources as billed
        var svcIds = preview.Lines.Where(l => l.SourceType == "ServiceOrder").Select(l => l.SourceId).ToList();
        var labIds = preview.Lines.Where(l => l.SourceType == "LabOrder").Select(l => l.SourceId).ToList();
        var imgIds = preview.Lines.Where(l => l.SourceType == "ImagingOrder").Select(l => l.SourceId).ToList();
        var dispIds = preview.Lines.Where(l => l.SourceType == "PrescriptionDispensing").Select(l => l.SourceId).ToList();

        if (svcIds.Count > 0) await db.ServiceOrders.Where(x => svcIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsBilled, true), cancellationToken);
        if (labIds.Count > 0) await db.LabOrders.Where(x => labIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsBilled, true), cancellationToken);
        if (imgIds.Count > 0) await db.ImagingOrders.Where(x => imgIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsBilled, true), cancellationToken);
        if (dispIds.Count > 0) await db.PrescriptionDispensings.Where(x => dispIds.Contains(x.Id)).ExecuteUpdateAsync(s => s.SetProperty(x => x.IsBilled, true), cancellationToken);

        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("InvoiceCreated", "Invoice", invoice.Id.ToString(),
            afterSummary: new { invoice.InvoiceCode, invoice.TotalAmount, invoice.PatientResponsibilityAmount },
            facilityId: facilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(invoice.Id, $"Đã lập hóa đơn {invoice.InvoiceCode} tổng tiền {invoice.TotalAmount:N0} đ.");
    }

    public async Task<OperationResult<Guid>> RecordPaymentAsync(
        Guid invoiceId, decimal amount, PaymentMethod method, bool isAdvance = false, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (amount <= 0) return OperationResult<Guid>.Fail("Số tiền thanh toán phải lớn hơn 0.");

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null) return OperationResult<Guid>.Fail("Không tìm thấy hóa đơn.");
        if (invoice.Status is InvoiceStatus.Cancelled or InvoiceStatus.Refunded)
            return OperationResult<Guid>.Fail("Hóa đơn đã hủy hoặc đã hoàn tiền.");

        var remaining = invoice.PatientResponsibilityAmount - invoice.PaidAmount;
        if (!isAdvance && amount > remaining)
        {
            return OperationResult<Guid>.Fail($"Số tiền thu ({amount:N0} đ) vượt quá số dư phải thu còn lại ({remaining:N0} đ).");
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var receiptNumber = await codeGenerator.GenerateNextCodeAsync("PT", "Payment", cancellationToken);
        var allocated = Math.Min(amount, Math.Max(0, remaining));

        var payment = new Payment
        {
            ReceiptNumber = receiptNumber,
            PatientId = invoice.PatientId,
            FacilityId = invoice.FacilityId,
            Amount = amount,
            PaymentMethod = method,
            Status = PaymentStatus.Completed,
            IsAdvance = isAdvance,
            PaidAtUtc = DateTimeOffset.UtcNow,
            CollectedBy = currentUserService.FullName,
            Notes = notes?.Trim()
        };

        if (allocated > 0)
        {
            payment.Allocations.Add(new PaymentAllocation
            {
                PaymentId = payment.Id,
                InvoiceId = invoice.Id,
                AllocatedAmount = allocated
            });
            invoice.PaidAmount += allocated;
        }

        invoice.Status = invoice.PaidAmount >= invoice.PatientResponsibilityAmount
            ? InvoiceStatus.Paid
            : invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Issued;

        db.Payments.Add(payment);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        if (method != PaymentMethod.Cash)
        {
            await paymentGateway.RecordSandboxTransactionAsync(receiptNumber, amount, method.ToString(), receiptNumber, cancellationToken);
        }

        await auditService.LogAsync("PaymentCollected", "Payment", payment.Id.ToString(),
            afterSummary: new { payment.ReceiptNumber, invoice.InvoiceCode, payment.Amount, Method = method.ToString() },
            facilityId: invoice.FacilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(payment.Id, $"Đã thu {amount:N0} đ - Phiếu thu {receiptNumber}.");
    }

    public async Task<OperationResult<Guid>> ProcessRefundAsync(Guid paymentId, decimal refundAmount, string reason, CancellationToken cancellationToken = default)
    {
        if (refundAmount <= 0) return OperationResult<Guid>.Fail("Số tiền hoàn phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult<Guid>.Fail("Phải nhập lý do hoàn tiền.");

        var payment = await db.Payments
            .Include(p => p.Refunds)
            .Include(p => p.Allocations)
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);
        if (payment is null) return OperationResult<Guid>.Fail("Không tìm thấy giao dịch thanh toán gốc.");

        var alreadyRefunded = payment.Refunds
            .Where(r => r.Status == RefundStatus.Completed || r.Status == RefundStatus.Approved)
            .Sum(r => r.Amount);
        var maxRefundable = payment.Amount - alreadyRefunded;

        if (refundAmount > maxRefundable)
        {
            return OperationResult<Guid>.Fail($"Số tiền hoàn ({refundAmount:N0} đ) vượt quá số tiền còn có thể hoàn ({maxRefundable:N0} đ).");
        }

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var primaryAlloc = payment.Allocations.FirstOrDefault();
        var refund = new Refund
        {
            RefundCode = await codeGenerator.GenerateNextCodeAsync("HT", "Refund", cancellationToken),
            PaymentId = payment.Id,
            InvoiceId = primaryAlloc?.InvoiceId,
            Amount = refundAmount,
            Reason = reason.Trim(),
            Status = RefundStatus.Completed,
            RequestedBy = currentUserService.FullName,
            ApprovedBy = currentUserService.FullName,
            RefundedAtUtc = DateTimeOffset.UtcNow
        };
        db.Refunds.Add(refund);

        payment.Status = (alreadyRefunded + refundAmount) >= payment.Amount
            ? PaymentStatus.Refunded
            : PaymentStatus.PartiallyRefunded;

        if (primaryAlloc is not null)
        {
            var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == primaryAlloc.InvoiceId, cancellationToken);
            if (invoice is not null)
            {
                invoice.RefundedAmount += refundAmount;
                invoice.PaidAmount = Math.Max(0, invoice.PaidAmount - refundAmount);
                invoice.Status = invoice.PaidAmount == 0 && invoice.RefundedAmount >= invoice.TotalAmount
                    ? InvoiceStatus.Refunded
                    : invoice.PaidAmount >= invoice.PatientResponsibilityAmount
                        ? InvoiceStatus.Paid
                        : invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Issued;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("RefundProcessed", "Refund", refund.Id.ToString(), reason: reason,
            afterSummary: new { refund.RefundCode, payment.ReceiptNumber, refund.Amount },
            facilityId: payment.FacilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(refund.Id, $"Đã hoàn tiền {refundAmount:N0} đ ({refund.RefundCode}).");
    }

    public async Task<OperationResult> ApplyAdjustmentAsync(Guid invoiceId, decimal adjustmentAmount, string reason, CancellationToken cancellationToken = default)
    {
        if (adjustmentAmount == 0) return OperationResult.Fail("Số tiền điều chỉnh phải khác 0.");
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult.Fail("Phải nhập lý do điều chỉnh.");

        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null) return OperationResult.Fail("Không tìm thấy hóa đơn.");

        if (invoice.PatientResponsibilityAmount + adjustmentAmount < 0)
            return OperationResult.Fail("Điều chỉnh không được làm tổng phải thu nhỏ hơn 0.");

        db.BillingAdjustments.Add(new BillingAdjustment
        {
            InvoiceId = invoice.Id,
            AdjustmentAmount = adjustmentAmount,
            Reason = reason.Trim(),
            ApprovedBy = currentUserService.FullName,
            AdjustedAtUtc = DateTimeOffset.UtcNow
        });

        invoice.TotalAmount += adjustmentAmount;
        invoice.PatientResponsibilityAmount += adjustmentAmount;
        invoice.Status = invoice.PaidAmount >= invoice.PatientResponsibilityAmount
            ? InvoiceStatus.Paid
            : invoice.PaidAmount > 0 ? InvoiceStatus.PartiallyPaid : InvoiceStatus.Issued;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("BillingAdjusted", "Invoice", invoice.Id.ToString(), reason: reason,
            afterSummary: new { adjustmentAmount, invoice.PatientResponsibilityAmount },
            facilityId: invoice.FacilityId, cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã ghi nhận điều chỉnh tài chính.");
    }

    public async Task<OperationResult<string>> IssueDraftEInvoiceDemoAsync(Guid invoiceId, CancellationToken cancellationToken = default)
    {
        var invoice = await db.Invoices.FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null) return OperationResult<string>.Fail("Không tìm thấy hóa đơn.");

        var res = await eInvoiceIntegration.IssueDraftEInvoiceDemoAsync(invoice.Id, invoice.InvoiceCode, invoice.TotalAmount, invoice.InvoiceCode, cancellationToken);
        if (res.Succeeded && !string.IsNullOrWhiteSpace(res.Data))
        {
            invoice.EInvoiceReference = res.Data;
            await db.SaveChangesAsync(cancellationToken);
        }
        return res;
    }

    public async Task<OperationResult<Guid>> CreateInsuranceClaimStagingAsync(Guid invoiceId, Guid? insuranceProfileId, decimal claimedAmount, string? notes, CancellationToken cancellationToken = default)
    {
        var invoice = await db.Invoices.AsNoTracking().FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);
        if (invoice is null) return OperationResult<Guid>.Fail("Không tìm thấy hóa đơn.");

        var claim = new InsuranceClaim
        {
            ClaimCode = await codeGenerator.GenerateNextCodeAsync("BHYT", "InsuranceClaim", cancellationToken),
            InvoiceId = invoiceId,
            PatientId = invoice.PatientId,
            InsuranceProfileId = insuranceProfileId,
            ClaimedAmount = claimedAmount,
            Status = "Staging",
            Notes = notes?.Trim()
        };

        var adapterRes = await insuranceIntegration.SubmitClaimStagingDemoAsync(claim.Id, claim.ClaimCode, claimedAmount, claim.ClaimCode, cancellationToken);
        claim.IntegrationMessageRef = adapterRes.Data;
        if (adapterRes.Succeeded) claim.Status = "SubmittedSandbox";

        db.InsuranceClaims.Add(claim);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(claim.Id, $"Đã tạo hồ sơ staging BHYT {claim.ClaimCode} [Demo/Sandbox].");
    }

    public async Task<CashierDailyReportDto> GetCashierDailyReportAsync(Guid facilityId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var startUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
        var endUtc = new DateTimeOffset(date.ToDateTime(TimeOnly.MaxValue), TimeSpan.FromHours(7)).ToUniversalTime();

        var payments = await db.Payments.AsNoTracking()
            .Where(p => p.FacilityId == facilityId && p.PaidAtUtc >= startUtc && p.PaidAtUtc <= endUtc)
            .ToListAsync(cancellationToken);

        var refunds = await db.Refunds.AsNoTracking()
            .Where(r => r.RefundedAtUtc >= startUtc && r.RefundedAtUtc <= endUtc && r.Status == RefundStatus.Completed)
            .ToListAsync(cancellationToken);

        var totalCollected = payments.Sum(p => p.Amount);
        var totalRefunded = refunds.Sum(r => r.Amount);

        return new CashierDailyReportDto(
            date,
            payments.Count,
            totalCollected,
            payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount),
            payments.Where(p => p.PaymentMethod == PaymentMethod.BankTransfer).Sum(p => p.Amount),
            payments.Where(p => p.PaymentMethod == PaymentMethod.Card || p.PaymentMethod == PaymentMethod.EWalletSandbox).Sum(p => p.Amount),
            totalRefunded,
            totalCollected - totalRefunded);
    }
}
