using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Pharmacy;

namespace SmartHospital.Application.Pharmacy;

public record PrescriptionWorklistDto(
    Guid PrescriptionId,
    string PrescriptionCode,
    Guid EncounterId,
    Guid PatientId,
    string PatientCode,
    string PatientName,
    string DoctorName,
    DateTimeOffset PrescribedAtUtc,
    PrescriptionStatus Status,
    int ItemCount);

public record MedicineStockSummaryDto(
    Guid BatchId,
    Guid WarehouseId,
    string WarehouseName,
    Guid MedicineId,
    string MedicineCode,
    string MedicineName,
    string BaseUnit,
    string BatchNumber,
    DateOnly ExpiryDate,
    decimal QuantityOnHand,
    decimal MinStockLevel,
    bool IsLowStock,
    bool IsExpiringSoon,
    bool IsExpired);

public record DispenseItemInput(Guid PrescriptionItemId, decimal QuantityToDispense);

public interface IPharmacyAndInventoryService
{
    Task<PagedResult<PrescriptionWorklistDto>> GetPrescriptionWorklistAsync(PagedRequest request, Guid? facilityId = null, PrescriptionStatus? status = null, CancellationToken cancellationToken = default);
    Task<Prescription?> GetPrescriptionDetailAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicineStockSummaryDto>> GetMedicineStockAsync(Guid? warehouseId = null, bool onlyWarnings = false, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> ReceiveMedicineBatchAsync(Guid facilityId, Guid warehouseId, Guid medicineId, string batchNumber, DateOnly expiryDate, decimal quantity, decimal unitCost, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> AdjustMedicineStockAsync(Guid facilityId, Guid warehouseId, Guid batchId, decimal deltaQuantity, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> DispensePrescriptionAsync(Guid prescriptionId, Guid warehouseId, IReadOnlyList<DispenseItemInput> items, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StockTransaction>> GetStockTransactionsAsync(Guid? warehouseId = null, int take = 50, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PurchaseRequest>> GetPurchaseRequestsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreatePurchaseRequestAsync(Guid facilityId, Guid warehouseId, Guid? supplierId, string itemsSummary, decimal estimatedTotal, string? notes, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdatePurchaseRequestStatusAsync(Guid purchaseRequestId, PurchaseRequestStatus status, CancellationToken cancellationToken = default);
}

public class PharmacyAndInventoryService(
    IHospitalDbContext db,
    ICodeGeneratorService codeGenerator,
    ICurrentUserService currentUserService,
    IAuditService auditService) : IPharmacyAndInventoryService
{
    public async Task<PagedResult<PrescriptionWorklistDto>> GetPrescriptionWorklistAsync(PagedRequest request, Guid? facilityId = null, PrescriptionStatus? status = null, CancellationToken cancellationToken = default)
    {
        var query = from pr in db.Prescriptions.AsNoTracking()
                    join p in db.Patients.AsNoTracking() on pr.PatientId equals p.Id
                    select new { pr, p };

        if (facilityId.HasValue) query = query.Where(x => x.pr.FacilityId == facilityId.Value);
        if (status.HasValue) query = query.Where(x => x.pr.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.pr.PrescriptionCode.Contains(term) || x.p.PatientCode.Contains(term) || x.p.FullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.pr.PrescribedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(x => new PrescriptionWorklistDto(
                x.pr.Id, x.pr.PrescriptionCode, x.pr.EncounterId, x.pr.PatientId,
                x.p.PatientCode, x.p.FullName, x.pr.DoctorName, x.pr.PrescribedAtUtc,
                x.pr.Status, x.pr.Items.Count))
            .ToListAsync(cancellationToken);

        return new PagedResult<PrescriptionWorklistDto> { Items = items, TotalCount = total, Page = request.Page, PageSize = request.PageSize };
    }

    public async Task<Prescription?> GetPrescriptionDetailAsync(Guid prescriptionId, CancellationToken cancellationToken = default)
        => await db.Prescriptions.AsNoTracking().Include(p => p.Items).FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);

    public async Task<IReadOnlyList<MedicineStockSummaryDto>> GetMedicineStockAsync(Guid? warehouseId = null, bool onlyWarnings = false, CancellationToken cancellationToken = default)
    {
        var today = HospitalTime.TodayVietnam();
        var soon = today.AddDays(60);

        var query = from b in db.MedicineBatches.AsNoTracking()
                    join m in db.Medicines.AsNoTracking() on b.MedicineId equals m.Id
                    join w in db.Warehouses.AsNoTracking() on b.WarehouseId equals w.Id
                    where b.IsActive && m.IsActive
                    select new { b, m, w };

        if (warehouseId.HasValue) query = query.Where(x => x.b.WarehouseId == warehouseId.Value);

        var rows = await query.OrderBy(x => x.m.Name).ThenBy(x => x.b.ExpiryDate).ToListAsync(cancellationToken);
        var list = rows.Select(x =>
        {
            var isLow = x.b.QuantityOnHand <= x.m.MinStockLevel;
            var isExpired = x.b.ExpiryDate < today;
            var isExpiring = !isExpired && x.b.ExpiryDate <= soon;
            return new MedicineStockSummaryDto(
                x.b.Id, x.w.Id, x.w.Name, x.m.Id, x.m.Code, x.m.Name, x.m.BaseUnit,
                x.b.BatchNumber, x.b.ExpiryDate, x.b.QuantityOnHand, x.m.MinStockLevel,
                isLow, isExpiring, isExpired);
        });

        if (onlyWarnings)
        {
            list = list.Where(x => x.IsLowStock || x.IsExpiringSoon || x.IsExpired);
        }

        return list.ToList();
    }

    public async Task<OperationResult<Guid>> ReceiveMedicineBatchAsync(
        Guid facilityId, Guid warehouseId, Guid medicineId, string batchNumber, DateOnly expiryDate,
        decimal quantity, decimal unitCost, string? notes, CancellationToken cancellationToken = default)
    {
        if (quantity <= 0) return OperationResult<Guid>.Fail("Số lượng nhập kho phải lớn hơn 0.");
        if (string.IsNullOrWhiteSpace(batchNumber)) return OperationResult<Guid>.Fail("Số lô là bắt buộc.");

        var med = await db.Medicines.AsNoTracking().FirstOrDefaultAsync(m => m.Id == medicineId, cancellationToken);
        if (med is null) return OperationResult<Guid>.Fail("Không tìm thấy thuốc.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var cleanBatch = batchNumber.Trim().ToUpperInvariant();
        var batch = await db.MedicineBatches.FirstOrDefaultAsync(
            b => b.WarehouseId == warehouseId && b.MedicineId == medicineId && b.BatchNumber == cleanBatch, cancellationToken);

        if (batch is null)
        {
            batch = new MedicineBatch
            {
                WarehouseId = warehouseId,
                MedicineId = medicineId,
                BatchNumber = cleanBatch,
                ExpiryDate = expiryDate,
                QuantityOnHand = quantity,
                UnitCost = unitCost
            };
            db.MedicineBatches.Add(batch);
        }
        else
        {
            batch.QuantityOnHand += quantity;
            batch.ExpiryDate = expiryDate;
            batch.UnitCost = unitCost;
        }

        var stockTx = new StockTransaction
        {
            TransactionCode = await codeGenerator.GenerateNextCodeAsync("NK", "StockTx", cancellationToken),
            FacilityId = facilityId,
            WarehouseId = warehouseId,
            TransactionType = StockTransactionType.Receipt,
            TransactionDateUtc = DateTimeOffset.UtcNow,
            Reason = notes?.Trim() ?? "Nhập kho thuốc",
            PerformedBy = currentUserService.FullName
        };
        stockTx.Items.Add(new StockTransactionItem
        {
            StockTransactionId = stockTx.Id,
            MedicineId = med.Id,
            MedicineBatchId = batch.Id,
            ItemCode = med.Code,
            ItemName = med.Name,
            Quantity = quantity,
            UnitCost = unitCost
        });

        db.StockTransactions.Add(stockTx);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("StockReceipt", "StockTransaction", stockTx.Id.ToString(),
            afterSummary: new { stockTx.TransactionCode, med.Code, cleanBatch, quantity },
            facilityId: facilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(stockTx.Id, $"Đã nhập kho {quantity} {med.BaseUnit} ({med.Name}).");
    }

    public async Task<OperationResult<Guid>> AdjustMedicineStockAsync(
        Guid facilityId, Guid warehouseId, Guid batchId, decimal deltaQuantity, string reason, CancellationToken cancellationToken = default)
    {
        if (deltaQuantity == 0) return OperationResult<Guid>.Fail("Số lượng điều chỉnh phải khác 0.");
        if (string.IsNullOrWhiteSpace(reason)) return OperationResult<Guid>.Fail("Phải nhập lý do điều chỉnh tồn kho.");

        var batch = await db.MedicineBatches.Include(b => b.Medicine)
            .FirstOrDefaultAsync(b => b.Id == batchId && b.WarehouseId == warehouseId, cancellationToken);
        if (batch is null || batch.Medicine is null) return OperationResult<Guid>.Fail("Không tìm thấy lô thuốc trong kho.");

        if (batch.QuantityOnHand + deltaQuantity < 0)
            return OperationResult<Guid>.Fail($"Không thể điều chỉnh âm tồn kho (Tồn hiện tại: {batch.QuantityOnHand}).");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var beforeQty = batch.QuantityOnHand;
        batch.QuantityOnHand += deltaQuantity;

        var stockTx = new StockTransaction
        {
            TransactionCode = await codeGenerator.GenerateNextCodeAsync("DCK", "StockTx", cancellationToken),
            FacilityId = facilityId,
            WarehouseId = warehouseId,
            TransactionType = deltaQuantity > 0 ? StockTransactionType.AdjustmentIncrease : StockTransactionType.AdjustmentDecrease,
            TransactionDateUtc = DateTimeOffset.UtcNow,
            Reason = reason.Trim(),
            PerformedBy = currentUserService.FullName,
            ApprovedBy = currentUserService.FullName
        };
        stockTx.Items.Add(new StockTransactionItem
        {
            StockTransactionId = stockTx.Id,
            MedicineId = batch.MedicineId,
            MedicineBatchId = batch.Id,
            ItemCode = batch.Medicine.Code,
            ItemName = batch.Medicine.Name,
            Quantity = Math.Abs(deltaQuantity),
            UnitCost = batch.UnitCost
        });

        db.StockTransactions.Add(stockTx);
        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("StockAdjusted", "StockTransaction", stockTx.Id.ToString(), reason: reason,
            beforeSummary: new { QuantityOnHand = beforeQty },
            afterSummary: new { QuantityOnHand = batch.QuantityOnHand },
            facilityId: facilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(stockTx.Id, $"Đã điều chỉnh tồn kho thành công (Tồn mới: {batch.QuantityOnHand}).");
    }

    public async Task<OperationResult<Guid>> DispensePrescriptionAsync(
        Guid prescriptionId, Guid warehouseId, IReadOnlyList<DispenseItemInput> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0) return OperationResult<Guid>.Fail("Chưa chọn thuốc cần cấp phát.");

        var prescription = await db.Prescriptions
            .Include(p => p.Items)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId, cancellationToken);
        if (prescription is null) return OperationResult<Guid>.Fail("Không tìm thấy đơn thuốc.");
        if (prescription.Status == PrescriptionStatus.Cancelled)
            return OperationResult<Guid>.Fail("Đơn thuốc đã bị hủy.");

        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);

        var today = HospitalTime.TodayVietnam();
        var stockTx = new StockTransaction
        {
            TransactionCode = await codeGenerator.GenerateNextCodeAsync("XK", "StockTx", cancellationToken),
            FacilityId = prescription.FacilityId,
            WarehouseId = warehouseId,
            TransactionType = StockTransactionType.DispenseOut,
            TransactionDateUtc = DateTimeOffset.UtcNow,
            ReferenceType = "Prescription",
            ReferenceId = prescription.Id,
            Reason = $"Cấp phát đơn thuốc {prescription.PrescriptionCode}",
            PerformedBy = currentUserService.FullName
        };

        var dispensing = new PrescriptionDispensing
        {
            DispensingCode = await codeGenerator.GenerateNextCodeAsync("CP", "Dispensing", cancellationToken),
            PrescriptionId = prescription.Id,
            PatientId = prescription.PatientId,
            EncounterId = prescription.EncounterId,
            WarehouseId = warehouseId,
            StockTransactionId = stockTx.Id,
            DispensedAtUtc = DateTimeOffset.UtcNow,
            DispensedBy = currentUserService.FullName
        };

        foreach (var input in items.Where(i => i.QuantityToDispense > 0))
        {
            var pItem = prescription.Items.FirstOrDefault(x => x.Id == input.PrescriptionItemId);
            if (pItem is null)
                return OperationResult<Guid>.Fail("Thuốc cấp phát không thuộc đơn thuốc.");

            var remainingToDispense = pItem.QuantityPrescribed - pItem.QuantityDispensed;
            if (input.QuantityToDispense > remainingToDispense)
                return OperationResult<Guid>.Fail($"Số lượng cấp '{pItem.MedicineName}' ({input.QuantityToDispense}) vượt quá số lượng còn lại của đơn ({remainingToDispense}).");

            // FEFO: earliest valid expiry batch first
            var batches = await db.MedicineBatches
                .Where(b => b.WarehouseId == warehouseId && b.MedicineId == pItem.MedicineId && b.QuantityOnHand > 0 && b.ExpiryDate >= today && b.IsActive)
                .OrderBy(b => b.ExpiryDate)
                .ToListAsync(cancellationToken);

            var totalAvailable = batches.Sum(b => b.QuantityOnHand);
            if (totalAvailable < input.QuantityToDispense)
                return OperationResult<Guid>.Fail($"Tồn kho thuốc '{pItem.MedicineName}' không đủ (Còn: {totalAvailable}, Cần: {input.QuantityToDispense}).");

            var need = input.QuantityToDispense;
            foreach (var batch in batches)
            {
                if (need <= 0) break;
                var take = Math.Min(batch.QuantityOnHand, need);
                batch.QuantityOnHand -= take;
                need -= take;

                stockTx.Items.Add(new StockTransactionItem
                {
                    StockTransactionId = stockTx.Id,
                    MedicineId = pItem.MedicineId,
                    MedicineBatchId = batch.Id,
                    ItemCode = pItem.MedicineCode,
                    ItemName = pItem.MedicineName,
                    Quantity = take,
                    UnitCost = batch.UnitCost
                });

                dispensing.Items.Add(new PrescriptionDispensingItem
                {
                    PrescriptionDispensingId = dispensing.Id,
                    PrescriptionItemId = pItem.Id,
                    MedicineId = pItem.MedicineId,
                    MedicineBatchId = batch.Id,
                    QuantityDispensed = take,
                    UnitPriceSnapshot = pItem.UnitPriceSnapshot
                });
            }

            pItem.QuantityDispensed += input.QuantityToDispense;
        }

        if (dispensing.Items.Count == 0)
            return OperationResult<Guid>.Fail("Không có thuốc nào có số lượng cấp phát > 0.");

        prescription.Status = prescription.Items.All(i => i.QuantityDispensed >= i.QuantityPrescribed)
            ? PrescriptionStatus.Dispensed
            : PrescriptionStatus.PartiallyDispensed;

        db.StockTransactions.Add(stockTx);
        db.PrescriptionDispensings.Add(dispensing);

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        await auditService.LogAsync("PrescriptionDispensed", "PrescriptionDispensing", dispensing.Id.ToString(),
            afterSummary: new { dispensing.DispensingCode, prescription.PrescriptionCode, stockTx.TransactionCode },
            facilityId: prescription.FacilityId, cancellationToken: cancellationToken);

        return OperationResult<Guid>.Ok(dispensing.Id, $"Đã cấp phát thành công ({dispensing.DispensingCode}).");
    }

    public async Task<IReadOnlyList<StockTransaction>> GetStockTransactionsAsync(Guid? warehouseId = null, int take = 50, CancellationToken cancellationToken = default)
    {
        var q = db.StockTransactions.AsNoTracking().Include(t => t.Items).AsQueryable();
        if (warehouseId.HasValue) q = q.Where(t => t.WarehouseId == warehouseId.Value);
        return await q.OrderByDescending(t => t.TransactionDateUtc).Take(take).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetInventoryItemsAsync(CancellationToken cancellationToken = default)
        => await db.InventoryItems.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PurchaseRequest>> GetPurchaseRequestsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        var q = db.PurchaseRequests.AsNoTracking();
        if (facilityId.HasValue) q = q.Where(r => r.FacilityId == facilityId.Value);
        return await q.OrderByDescending(r => r.RequestedAtUtc).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult<Guid>> CreatePurchaseRequestAsync(Guid facilityId, Guid warehouseId, Guid? supplierId, string itemsSummary, decimal estimatedTotal, string? notes, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(itemsSummary)) return OperationResult<Guid>.Fail("Nội dung đề xuất mua hàng là bắt buộc.");

        var pr = new PurchaseRequest
        {
            RequestCode = await codeGenerator.GenerateNextCodeAsync("PR", "PurchaseRequest", cancellationToken),
            FacilityId = facilityId,
            WarehouseId = warehouseId,
            SupplierId = supplierId,
            RequestedBy = currentUserService.FullName,
            ItemsSummary = itemsSummary.Trim(),
            EstimatedTotal = estimatedTotal,
            Notes = notes?.Trim(),
            Status = PurchaseRequestStatus.Submitted
        };
        db.PurchaseRequests.Add(pr);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(pr.Id, $"Đã tạo đề xuất mua hàng {pr.RequestCode}.");
    }

    public async Task<OperationResult> UpdatePurchaseRequestStatusAsync(Guid purchaseRequestId, PurchaseRequestStatus status, CancellationToken cancellationToken = default)
    {
        var pr = await db.PurchaseRequests.FirstOrDefaultAsync(r => r.Id == purchaseRequestId, cancellationToken);
        if (pr is null) return OperationResult.Fail("Không tìm thấy đề xuất mua hàng.");
        pr.Status = status;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok($"Đã cập nhật đề xuất {pr.RequestCode} sang {status}.");
    }
}
