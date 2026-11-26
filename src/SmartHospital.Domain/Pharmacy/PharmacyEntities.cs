using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Pharmacy;

public class Medicine : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ActiveIngredient { get; set; } = string.Empty;
    public string? Strength { get; set; }
    public string DosageForm { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "Viên";
    public string DefaultRoute { get; set; } = "Uống";
    public decimal DefaultUnitPrice { get; set; }
    public decimal MinStockLevel { get; set; } = 10;
    public bool IsControlledSubstance { get; set; }
    public bool IsDemoData { get; set; }
}

public class MedicineUnit : AuditableEntity
{
    public Guid MedicineId { get; set; }
    public string UnitName { get; set; } = string.Empty;
    public decimal ConversionRateToBase { get; set; } = 1;
}

public class Warehouse : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WarehouseType { get; set; } = "Pharmacy"; // Pharmacy, MainSupply, DepartmentSupply
}

public class WarehouseLocation : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Warehouse? Warehouse { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class MedicineBatch : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Guid? WarehouseLocationId { get; set; }
    public Guid MedicineId { get; set; }
    public Medicine? Medicine { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
}

public class StockTransaction : AuditableEntity
{
    public string TransactionCode { get; set; } = string.Empty;
    public Guid FacilityId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? TargetWarehouseId { get; set; }
    public StockTransactionType TransactionType { get; set; }
    public DateTimeOffset TransactionDateUtc { get; set; } = DateTimeOffset.UtcNow;
    public string? ReferenceType { get; set; }
    public Guid? ReferenceId { get; set; }
    public string? Reason { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public string? ApprovedBy { get; set; }

    public ICollection<StockTransactionItem> Items { get; set; } = [];
}

public class StockTransactionItem : AuditableEntity
{
    public Guid StockTransactionId { get; set; }
    public StockTransaction? StockTransaction { get; set; }
    public Guid? MedicineId { get; set; }
    public Guid? MedicineBatchId { get; set; }
    public Guid? InventoryItemId { get; set; }
    public Guid? InventoryBatchId { get; set; }
    public string ItemCode { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; } // Positive magnitude; direction determined by TransactionType
    public decimal UnitCost { get; set; }
}

public class PrescriptionDispensing : AuditableEntity
{
    public string DispensingCode { get; set; } = string.Empty;
    public Guid PrescriptionId { get; set; }
    public Guid PatientId { get; set; }
    public Guid EncounterId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid StockTransactionId { get; set; }
    public DateTimeOffset DispensedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string DispensedBy { get; set; } = string.Empty;
    public bool IsBilled { get; set; }

    public ICollection<PrescriptionDispensingItem> Items { get; set; } = [];
}

public class PrescriptionDispensingItem : AuditableEntity
{
    public Guid PrescriptionDispensingId { get; set; }
    public PrescriptionDispensing? PrescriptionDispensing { get; set; }
    public Guid PrescriptionItemId { get; set; }
    public Guid MedicineId { get; set; }
    public Guid MedicineBatchId { get; set; }
    public decimal QuantityDispensed { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
}

public class Supplier : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
}

public class InventoryItem : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = "Vật tư tiêu hao";
    public string Unit { get; set; } = "Cái";
    public decimal MinStockLevel { get; set; } = 10;
    public decimal MaxStockLevel { get; set; } = 500;
    public decimal DefaultUnitPrice { get; set; }
}

public class InventoryBatch : AuditableEntity
{
    public Guid WarehouseId { get; set; }
    public Guid InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public string BatchNumber { get; set; } = string.Empty;
    public DateOnly? ExpiryDate { get; set; }
    public decimal QuantityOnHand { get; set; }
    public decimal UnitCost { get; set; }
}

public class PurchaseRequest : AuditableEntity
{
    public string RequestCode { get; set; } = string.Empty;
    public Guid FacilityId { get; set; }
    public Guid WarehouseId { get; set; }
    public Guid? SupplierId { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string RequestedBy { get; set; } = string.Empty;
    public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Submitted;
    public string ItemsSummary { get; set; } = string.Empty;
    public decimal EstimatedTotal { get; set; }
    public string? Notes { get; set; }
}
