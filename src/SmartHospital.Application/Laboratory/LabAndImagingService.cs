using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Laboratory;

namespace SmartHospital.Application.Laboratory;

public record LabOrderWorklistDto(
    Guid LabOrderId,
    string OrderCode,
    Guid EncounterId,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    string OrderedByName,
    DateTimeOffset OrderedAtUtc,
    LabOrderStatus Status,
    string? SpecimenBarcode,
    SpecimenStatus? SpecimenStatus,
    int ItemCount,
    int VerifiedCount);

public record ImagingOrderWorklistDto(
    Guid ImagingOrderId,
    string OrderCode,
    Guid EncounterId,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    string ServiceName,
    string Modality,
    string OrderedByName,
    DateTimeOffset OrderedAtUtc,
    ImagingOrderStatus Status,
    bool IsVerified);

public interface ILabAndImagingService
{
    Task<PagedResult<LabOrderWorklistDto>> GetLabWorklistAsync(PagedRequest request, Guid? facilityId = null, LabOrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<LabOrder?> GetLabOrderDetailAsync(Guid labOrderId, CancellationToken cancellationToken = default);
    Task<OperationResult> CollectSpecimenAsync(Guid labOrderId, string? storageLocation, CancellationToken cancellationToken = default);
    Task<OperationResult> ReceiveSpecimenAsync(Guid labOrderId, CancellationToken cancellationToken = default);
    Task<OperationResult> RejectSpecimenAsync(Guid labOrderId, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult> EnterLabResultAsync(Guid labOrderItemId, string resultValue, decimal? numericValue, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> VerifyLabResultAsync(Guid labResultId, CancellationToken cancellationToken = default);
    Task<OperationResult> AmendLabResultAsync(Guid labResultId, string newResultValue, decimal? newNumericValue, string reason, CancellationToken cancellationToken = default);

    Task<PagedResult<ImagingOrderWorklistDto>> GetImagingWorklistAsync(PagedRequest request, Guid? facilityId = null, ImagingOrderStatus? status = null, CancellationToken cancellationToken = default);
    Task<ImagingOrder?> GetImagingOrderDetailAsync(Guid imagingOrderId, CancellationToken cancellationToken = default);
    Task<OperationResult> SaveImagingReportAsync(Guid imagingOrderId, string findings, string conclusion, bool verify, string? attachmentStorageKey = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> UploadImagingAttachmentAsync(Guid imagingOrderId, Stream content, string fileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default);
}

public class LabAndImagingService(
    IHospitalDbContext db,
    ICurrentUserService currentUserService,
    IAuditService auditService,
    IDocumentStorage documentStorage,
    IPacsIntegration pacsIntegration,
    INotificationProvider notificationProvider) : ILabAndImagingService
{
    public async Task<PagedResult<LabOrderWorklistDto>> GetLabWorklistAsync(PagedRequest request, Guid? facilityId = null, LabOrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = from o in db.LabOrders.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on o.PatientId equals p.Id
                    select new { o, p };

        if (facilityId.HasValue) query = query.Where(x => x.o.FacilityId == facilityId.Value);
        if (status.HasValue) query = query.Where(x => x.o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.o.OrderCode.Contains(term) || x.p.PatientCode.Contains(term) || x.p.FullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(x => x.o.OrderedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new
            {
                x.o.Id,
                x.o.OrderCode,
                x.o.EncounterId,
                x.o.PatientId,
                x.p.PatientCode,
                x.p.FullName,
                x.o.OrderedByName,
                x.o.OrderedAtUtc,
                x.o.Status,
                Specimen = x.o.Specimens.FirstOrDefault(),
                ItemCount = x.o.Items.Count,
                VerifiedCount = x.o.Items.Count(i => i.Result != null && i.Result.IsVerified)
            })
            .ToListAsync(cancellationToken);

        var items = rows.Select(r => new LabOrderWorklistDto(
            r.Id, r.OrderCode, r.EncounterId, r.PatientId, r.PatientCode, r.FullName,
            r.OrderedByName, r.OrderedAtUtc, r.Status,
            r.Specimen?.Barcode, r.Specimen?.Status, r.ItemCount, r.VerifiedCount)).ToList();

        return new PagedResult<LabOrderWorklistDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<LabOrder?> GetLabOrderDetailAsync(Guid labOrderId, CancellationToken cancellationToken = default)
        => await db.LabOrders.AsNoTracking()
            .Include(o => o.Specimens)
            .Include(o => o.Items)
            .ThenInclude(i => i.Result!)
            .ThenInclude(r => r.Versions)
            .FirstOrDefaultAsync(o => o.Id == labOrderId, cancellationToken);

    public async Task<OperationResult> CollectSpecimenAsync(Guid labOrderId, string? storageLocation, CancellationToken cancellationToken = default)
    {
        var order = await db.LabOrders.Include(o => o.Specimens).FirstOrDefaultAsync(o => o.Id == labOrderId, cancellationToken);
        if (order is null) return OperationResult.Fail("Không tìm thấy phiếu xét nghiệm.");

        var specimen = order.Specimens.FirstOrDefault();
        if (specimen is null) return OperationResult.Fail("Không tìm thấy thông tin mẫu bệnh phẩm.");

        specimen.Status = SpecimenStatus.Collected;
        specimen.CollectedAtUtc = DateTimeOffset.UtcNow;
        specimen.CollectedBy = currentUserService.FullName;
        specimen.StorageLocation = storageLocation?.Trim();
        order.Status = LabOrderStatus.Collected;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabSpecimenCollected", "LabOrder", order.Id.ToString(), afterSummary: new { specimen.Barcode }, facilityId: order.FacilityId, cancellationToken: cancellationToken);
        return OperationResult.Ok($"Đã ghi nhận lấy mẫu {specimen.Barcode}.");
    }

    public async Task<OperationResult> ReceiveSpecimenAsync(Guid labOrderId, CancellationToken cancellationToken = default)
    {
        var order = await db.LabOrders.Include(o => o.Specimens).FirstOrDefaultAsync(o => o.Id == labOrderId, cancellationToken);
        if (order is null) return OperationResult.Fail("Không tìm thấy phiếu xét nghiệm.");

        var specimen = order.Specimens.FirstOrDefault();
        if (specimen is null) return OperationResult.Fail("Không tìm thấy mẫu.");

        specimen.Status = SpecimenStatus.Received;
        specimen.ReceivedAtUtc = DateTimeOffset.UtcNow;
        specimen.ReceivedBy = currentUserService.FullName;
        order.Status = LabOrderStatus.InProgress;

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã nhận mẫu và chuyển sang thực hiện.");
    }

    public async Task<OperationResult> RejectSpecimenAsync(Guid labOrderId, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult.Fail("Phải nhập lý do từ chối mẫu.");
        var order = await db.LabOrders.Include(o => o.Specimens).FirstOrDefaultAsync(o => o.Id == labOrderId, cancellationToken);
        if (order is null) return OperationResult.Fail("Không tìm thấy phiếu xét nghiệm.");

        var specimen = order.Specimens.FirstOrDefault();
        if (specimen is not null)
        {
            specimen.Status = SpecimenStatus.Rejected;
            specimen.RejectionReason = reason.Trim();
        }
        order.Status = LabOrderStatus.Rejected;

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabSpecimenRejected", "LabOrder", order.Id.ToString(), reason: reason, facilityId: order.FacilityId, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã từ chối mẫu xét nghiệm.");
    }

    public async Task<OperationResult> EnterLabResultAsync(Guid labOrderItemId, string resultValue, decimal? numericValue, string? notes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resultValue)) return OperationResult.Fail("Giá trị kết quả là bắt buộc.");
        var item = await db.LabOrderItems
            .Include(i => i.LabOrder)
            .Include(i => i.Result)
            .FirstOrDefaultAsync(i => i.Id == labOrderItemId, cancellationToken);
        if (item is null || item.LabOrder is null) return OperationResult.Fail("Không tìm thấy mục xét nghiệm.");

        if (item.Result is not null && item.Result.IsVerified)
        {
            return OperationResult.Fail("Kết quả đã được duyệt và khóa phiên bản. Nếu cần sửa, vui lòng dùng chức năng Đính chính kết quả (Amend) với quyền duyệt.");
        }

        var test = await db.LabTests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == item.LabTestId, cancellationToken);
        var isAbnormal = EvaluateAbnormal(test, numericValue);

        if (item.Result is null)
        {
            item.Result = new LabResult
            {
                LabOrderItemId = item.Id,
                Unit = test?.Unit,
                ReferenceRange = test?.ReferenceRangeText,
                Version = 1
            };
            db.LabResults.Add(item.Result);
        }

        item.Result.ResultValue = resultValue.Trim();
        item.Result.NumericValue = numericValue;
        item.Result.IsAbnormal = isAbnormal;
        item.Result.Notes = notes?.Trim();
        item.Result.EnteredBy = currentUserService.FullName;
        item.Result.EnteredAtUtc = DateTimeOffset.UtcNow;

        item.Status = LabOrderStatus.PendingReview;
        item.LabOrder.Status = LabOrderStatus.PendingReview;

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã nhập kết quả xét nghiệm (chờ duyệt).");
    }

    public async Task<OperationResult> VerifyLabResultAsync(Guid labResultId, CancellationToken cancellationToken = default)
    {
        var result = await db.LabResults
            .Include(r => r.LabOrderItem!)
            .ThenInclude(i => i.LabOrder!)
            .ThenInclude(o => o.Items)
            .ThenInclude(i => i.Result)
            .FirstOrDefaultAsync(r => r.Id == labResultId, cancellationToken);

        if (result is null || result.LabOrderItem?.LabOrder is null)
            return OperationResult.Fail("Không tìm thấy kết quả xét nghiệm.");

        if (result.IsVerified) return OperationResult.Ok("Kết quả đã được duyệt trước đó.");

        result.IsVerified = true;
        result.VerifiedBy = currentUserService.FullName;
        result.VerifiedAtUtc = DateTimeOffset.UtcNow;
        result.LabOrderItem.Status = LabOrderStatus.Verified;

        db.LabResultVersions.Add(new LabResultVersion
        {
            LabResultId = result.Id,
            VersionNumber = result.Version,
            ResultValue = result.ResultValue,
            NumericValue = result.NumericValue,
            IsAbnormal = result.IsAbnormal,
            Reason = "Duyệt kết quả",
            ChangedBy = currentUserService.FullName,
            ChangedAtUtc = DateTimeOffset.UtcNow
        });

        var order = result.LabOrderItem.LabOrder;
        if (order.Items.All(i => i.Result != null && i.Result.IsVerified))
        {
            order.Status = LabOrderStatus.Verified;
            await notificationProvider.SendNotificationAsync(
                order.OrderedByUserId, order.PatientId, "InApp",
                $"Có kết quả xét nghiệm {order.OrderCode}",
                $"Phiếu xét nghiệm {order.OrderCode} đã được duyệt đầy đủ.",
                $"/Clinical/Encounters/Workspace/{order.EncounterId}",
                cancellationToken);
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabResultVerified", "LabResult", result.Id.ToString(),
            afterSummary: new { order.OrderCode, result.ResultValue, result.Version },
            facilityId: order.FacilityId, cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã duyệt và khóa kết quả xét nghiệm.");
    }

    public async Task<OperationResult> AmendLabResultAsync(Guid labResultId, string newResultValue, decimal? newNumericValue, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult.Fail("Phải nhập lý do đính chính kết quả.");
        if (string.IsNullOrWhiteSpace(newResultValue)) return OperationResult.Fail("Giá trị kết quả mới là bắt buộc.");

        var result = await db.LabResults
            .Include(r => r.LabOrderItem!)
            .ThenInclude(i => i.LabOrder)
            .FirstOrDefaultAsync(r => r.Id == labResultId, cancellationToken);

        if (result is null || result.LabOrderItem?.LabOrder is null)
            return OperationResult.Fail("Không tìm thấy kết quả xét nghiệm.");
        if (!result.IsVerified)
            return OperationResult.Fail("Kết quả chưa duyệt có thể sửa trực tiếp không cần đính chính.");

        var test = await db.LabTests.AsNoTracking().FirstOrDefaultAsync(t => t.Id == result.LabOrderItem.LabTestId, cancellationToken);
        var before = new { result.Version, result.ResultValue, result.NumericValue };

        result.Version += 1;
        result.ResultValue = newResultValue.Trim();
        result.NumericValue = newNumericValue;
        result.IsAbnormal = EvaluateAbnormal(test, newNumericValue);
        result.VerifiedBy = currentUserService.FullName;
        result.VerifiedAtUtc = DateTimeOffset.UtcNow;

        db.LabResultVersions.Add(new LabResultVersion
        {
            LabResultId = result.Id,
            VersionNumber = result.Version,
            ResultValue = result.ResultValue,
            NumericValue = result.NumericValue,
            IsAbnormal = result.IsAbnormal,
            Reason = reason.Trim(),
            ChangedBy = currentUserService.FullName,
            ChangedAtUtc = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabResultAmended", "LabResult", result.Id.ToString(), reason: reason,
            beforeSummary: before,
            afterSummary: new { result.Version, result.ResultValue, result.NumericValue },
            facilityId: result.LabOrderItem.LabOrder.FacilityId, cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã đính chính kết quả (phiên bản {result.Version}).");
    }

    public async Task<PagedResult<ImagingOrderWorklistDto>> GetImagingWorklistAsync(PagedRequest request, Guid? facilityId = null, ImagingOrderStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = from o in db.ImagingOrders.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on o.PatientId equals p.Id
                    select new { o, p };

        if (facilityId.HasValue) query = query.Where(x => x.o.FacilityId == facilityId.Value);
        if (status.HasValue) query = query.Where(x => x.o.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.o.OrderCode.Contains(term) || x.p.PatientCode.Contains(term) || x.p.FullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.o.OrderedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new ImagingOrderWorklistDto(
                x.o.Id, x.o.OrderCode, x.o.EncounterId, x.o.PatientId, x.p.PatientCode, x.p.FullName,
                x.o.ServiceName, x.o.Modality, x.o.OrderedByName, x.o.OrderedAtUtc,
                x.o.Status, x.o.Report != null && x.o.Report.IsVerified))
            .ToListAsync(cancellationToken);

        return new PagedResult<ImagingOrderWorklistDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<ImagingOrder?> GetImagingOrderDetailAsync(Guid imagingOrderId, CancellationToken cancellationToken = default)
        => await db.ImagingOrders.AsNoTracking()
            .Include(o => o.Report)
            .FirstOrDefaultAsync(o => o.Id == imagingOrderId, cancellationToken);

    public async Task<OperationResult> SaveImagingReportAsync(Guid imagingOrderId, string findings, string conclusion, bool verify, string? attachmentStorageKey = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(findings) || string.IsNullOrWhiteSpace(conclusion))
            return OperationResult.Fail("Mô tả và kết luận CĐHA là bắt buộc.");

        var order = await db.ImagingOrders.Include(o => o.Report).FirstOrDefaultAsync(o => o.Id == imagingOrderId, cancellationToken);
        if (order is null) return OperationResult.Fail("Không tìm thấy phiếu chỉ định CĐHA.");

        if (order.Report is not null && order.Report.IsVerified)
            return OperationResult.Fail("Kết quả CĐHA đã được duyệt khóa.");

        if (order.Report is null)
        {
            order.Report = new ImagingReport { ImagingOrderId = order.Id };
            db.ImagingReports.Add(order.Report);
        }

        var pacsResult = await pacsIntegration.GetDemoViewerUrlAsync(order.OrderCode, order.Modality, cancellationToken);

        order.Report.Findings = findings.Trim();
        order.Report.Conclusion = conclusion.Trim();
        order.Report.AttachmentStorageKey = attachmentStorageKey ?? order.Report.AttachmentStorageKey;
        order.Report.ExternalDemoViewerUrl = pacsResult.Data;
        order.Report.ReportedBy = currentUserService.FullName;
        order.Report.ReportedAtUtc = DateTimeOffset.UtcNow;

        if (verify)
        {
            order.Report.IsVerified = true;
            order.Report.VerifiedBy = currentUserService.FullName;
            order.Report.VerifiedAtUtc = DateTimeOffset.UtcNow;
            order.Status = ImagingOrderStatus.Verified;
        }
        else
        {
            order.Status = ImagingOrderStatus.Reported;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync(verify ? "ImagingReportVerified" : "ImagingReportSaved", "ImagingOrder", order.Id.ToString(),
            afterSummary: new { order.OrderCode, verify },
            facilityId: order.FacilityId, cancellationToken: cancellationToken);

        return OperationResult.Ok(verify ? "Đã duyệt kết quả CĐHA." : "Đã lưu báo cáo CĐHA.");
    }

    public async Task<OperationResult<Guid>> UploadImagingAttachmentAsync(Guid imagingOrderId, Stream content, string fileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
    {
        var order = await db.ImagingOrders.Include(o => o.Report).FirstOrDefaultAsync(o => o.Id == imagingOrderId, cancellationToken);
        if (order is null) return OperationResult<Guid>.Fail("Không tìm thấy chỉ định CĐHA.");

        var stored = await documentStorage.SaveAsync(content, fileName, contentType, sizeBytes, cancellationToken);
        var doc = new Document
        {
            StorageKey = stored.StorageKey,
            OriginalFileName = fileName,
            ContentType = contentType,
            SizeBytes = stored.SizeBytes,
            Sha256Checksum = stored.Sha256Checksum,
            EntityType = "ImagingOrder",
            EntityId = imagingOrderId,
            Description = $"Tài liệu đính kèm {order.OrderCode}"
        };
        db.Documents.Add(doc);

        if (order.Report is not null)
        {
            order.Report.AttachmentStorageKey = stored.StorageKey;
        }

        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(doc.Id, "Đã tải lên tài liệu CĐHA an toàn.");
    }

    private static bool EvaluateAbnormal(LabTest? test, decimal? numericValue)
    {
        if (test is null || !numericValue.HasValue) return false;
        if (test.ReferenceMin.HasValue && numericValue.Value < test.ReferenceMin.Value) return true;
        if (test.ReferenceMax.HasValue && numericValue.Value > test.ReferenceMax.Value) return true;
        return false;
    }
}
