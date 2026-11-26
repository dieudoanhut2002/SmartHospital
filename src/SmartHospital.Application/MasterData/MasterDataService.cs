using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Pharmacy;

namespace SmartHospital.Application.MasterData;

public record CatalogSnapshotDto(
    Hospital? Hospital,
    IReadOnlyList<Facility> Facilities,
    IReadOnlyList<Department> Departments,
    IReadOnlyList<Specialty> Specialties,
    IReadOnlyList<Room> Rooms,
    IReadOnlyList<Clinic> Clinics,
    IReadOnlyList<Bed> Beds,
    IReadOnlyList<ServiceCatalog> Services,
    IReadOnlyList<LabTest> LabTests,
    IReadOnlyList<ImagingService> ImagingServices,
    IReadOnlyList<Medicine> Medicines,
    IReadOnlyList<Warehouse> Warehouses,
    IReadOnlyList<Supplier> Suppliers,
    IReadOnlyList<InventoryItem> InventoryItems,
    IReadOnlyList<Diagnosis> Diagnoses,
    IReadOnlyList<ServicePriceList> PriceLists);

public interface IMasterDataService
{
    Task<CatalogSnapshotDto> GetCatalogSnapshotAsync(Guid? facilityId = null, bool includeInactive = true, CancellationToken cancellationToken = default);

    // Read (active)
    Task<Hospital?> GetHospitalAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Facility>> GetFacilitiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Department>> GetDepartmentsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Clinic>> GetClinicsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Room>> GetRoomsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Bed>> GetBedsAsync(Guid? facilityId = null, Guid? departmentId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceCatalog>> GetServicesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LabTest>> GetLabTestsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ImagingService>> GetImagingServicesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Medicine>> GetMedicinesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(Guid? facilityId = null, CancellationToken cancellationToken = default);

    // Hospital & Facility CRUD
    Task<OperationResult> UpdateHospitalAsync(Guid id, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateFacilityAsync(Guid hospitalId, string code, string name, string? phone, string? address, bool isDefault, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateFacilityAsync(Guid id, string name, string? phone, string? address, bool isDefault, bool isActive, CancellationToken cancellationToken = default);

    // Department CRUD
    Task<OperationResult<Guid>> CreateDepartmentAsync(Guid facilityId, string code, string name, DepartmentType type, string? location, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateDepartmentAsync(Guid id, string name, DepartmentType type, string? phone, string? location, bool isActive, CancellationToken cancellationToken = default);

    // Specialty CRUD
    Task<OperationResult<Guid>> CreateSpecialtyAsync(string code, string name, string? description, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateSpecialtyAsync(Guid id, string name, string? description, bool isActive, CancellationToken cancellationToken = default);

    // Room CRUD
    Task<OperationResult<Guid>> CreateRoomAsync(Guid facilityId, Guid departmentId, string code, string name, RoomType roomType, string? floor, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateRoomAsync(Guid id, Guid departmentId, string name, RoomType roomType, string? floor, bool isActive, CancellationToken cancellationToken = default);

    // Clinic CRUD
    Task<OperationResult<Guid>> CreateClinicAsync(Guid facilityId, Guid departmentId, Guid? specialtyId, Guid? roomId, string code, string name, string queuePrefix, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateClinicAsync(Guid id, Guid departmentId, Guid? specialtyId, Guid? roomId, string name, string queuePrefix, bool isActive, CancellationToken cancellationToken = default);

    // Bed CRUD
    Task<OperationResult<Guid>> CreateBedAsync(Guid facilityId, Guid departmentId, Guid roomId, string code, string name, decimal dailyRate, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateBedAsync(Guid id, Guid roomId, string name, decimal dailyRate, BedStatus status, bool isActive, CancellationToken cancellationToken = default);

    // ServiceCatalog CRUD
    Task<OperationResult<Guid>> CreateServiceAsync(string code, string name, string category, Guid? departmentId, decimal defaultPrice, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateServiceAsync(Guid id, string name, string category, Guid? departmentId, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default);

    // LabTest CRUD
    Task<OperationResult<Guid>> CreateLabTestAsync(string code, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateLabTestAsync(Guid id, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default);

    // ImagingService CRUD
    Task<OperationResult<Guid>> CreateImagingServiceAsync(string code, string name, string modality, decimal defaultPrice, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateImagingServiceAsync(Guid id, string name, string modality, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default);

    // Medicine CRUD
    Task<OperationResult<Guid>> CreateMedicineAsync(string code, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateMedicineAsync(Guid id, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, bool isActive, CancellationToken cancellationToken = default);

    // Warehouse, Supplier, InventoryItem, Diagnosis, PriceList CRUD
    Task<OperationResult<Guid>> CreateWarehouseAsync(Guid facilityId, Guid? departmentId, string code, string name, string warehouseType, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateWarehouseAsync(Guid id, string name, string warehouseType, bool isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreateSupplierAsync(string code, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateSupplierAsync(Guid id, string name, string? taxCode, string? phone, string? email, string? address, bool isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreateInventoryItemAsync(string code, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateInventoryItemAsync(Guid id, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, bool isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreateDiagnosisAsync(string icdCode, string name, string? chapter, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateDiagnosisAsync(Guid id, string name, string? chapter, bool isActive, CancellationToken cancellationToken = default);

    Task<OperationResult<Guid>> CreatePriceListAsync(string code, string name, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isDefault, CancellationToken cancellationToken = default);
    Task<OperationResult> UpsertServicePriceAsync(Guid priceListId, string itemType, Guid itemId, decimal unitPrice, CancellationToken cancellationToken = default);
}

public class MasterDataService(
    IHospitalDbContext db,
    IAuditService auditService) : IMasterDataService
{
    public async Task<CatalogSnapshotDto> GetCatalogSnapshotAsync(Guid? facilityId = null, bool includeInactive = true, CancellationToken cancellationToken = default)
    {
        var hospital = await db.Hospitals.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        var facilities = await db.Facilities.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var facId = facilityId ?? facilities.FirstOrDefault()?.Id;

        var depts = await db.Departments.AsNoTracking().Where(x => (includeInactive || x.IsActive) && (!facId.HasValue || x.FacilityId == facId)).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var specs = await db.Specialties.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var rooms = await db.Rooms.AsNoTracking().Include(r => r.Department).Where(x => (includeInactive || x.IsActive) && (!facId.HasValue || x.FacilityId == facId)).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var clinics = await db.Clinics.AsNoTracking().Where(x => (includeInactive || x.IsActive) && (!facId.HasValue || x.FacilityId == facId)).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var beds = await db.Beds.AsNoTracking().Include(b => b.Room).Where(x => (includeInactive || x.IsActive) && (!facId.HasValue || x.FacilityId == facId)).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var services = await db.ServiceCatalogs.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var labTests = await db.LabTests.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Category).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var imgSvcs = await db.ImagingServices.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Modality).ThenBy(x => x.Name).ToListAsync(cancellationToken);
        var medicines = await db.Medicines.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var warehouses = await db.Warehouses.AsNoTracking().Where(x => (includeInactive || x.IsActive) && (!facId.HasValue || x.FacilityId == facId)).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var suppliers = await db.Suppliers.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var invItems = await db.InventoryItems.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.Name).ToListAsync(cancellationToken);
        var diagnoses = await db.Diagnoses.AsNoTracking().Where(x => includeInactive || x.IsActive).OrderBy(x => x.IcdCode).ToListAsync(cancellationToken);
        var priceLists = await db.ServicePriceLists.AsNoTracking().Include(p => p.Prices).Where(x => includeInactive || x.IsActive).OrderByDescending(x => x.EffectiveFrom).ToListAsync(cancellationToken);

        return new CatalogSnapshotDto(
            hospital, facilities, depts, specs, rooms, clinics, beds,
            services, labTests, imgSvcs, medicines, warehouses, suppliers, invItems, diagnoses, priceLists);
    }

    public async Task<Hospital?> GetHospitalAsync(CancellationToken cancellationToken = default)
        => await db.Hospitals.AsNoTracking().FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Facility>> GetFacilitiesAsync(CancellationToken cancellationToken = default)
        => await db.Facilities.AsNoTracking().Where(f => f.IsActive).OrderBy(f => f.Code).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Department>> GetDepartmentsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Departments.AsNoTracking().Where(d => d.IsActive);
        if (facilityId.HasValue) q = q.Where(d => d.FacilityId == facilityId.Value);
        return await q.OrderBy(d => d.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Specialty>> GetSpecialtiesAsync(CancellationToken cancellationToken = default)
        => await db.Specialties.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Clinic>> GetClinicsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Clinics.AsNoTracking().Where(c => c.IsActive);
        if (facilityId.HasValue) q = q.Where(c => c.FacilityId == facilityId.Value);
        return await q.OrderBy(c => c.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Room>> GetRoomsAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Rooms.AsNoTracking().Include(r => r.Department).Where(r => r.IsActive);
        if (facilityId.HasValue) q = q.Where(r => r.FacilityId == facilityId.Value);
        return await q.OrderBy(r => r.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Bed>> GetBedsAsync(Guid? facilityId = null, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Beds.AsNoTracking().Include(b => b.Room).Where(b => b.IsActive);
        if (facilityId.HasValue) q = q.Where(b => b.FacilityId == facilityId.Value);
        if (departmentId.HasValue) q = q.Where(b => b.DepartmentId == departmentId.Value);
        return await q.OrderBy(b => b.Code).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCatalog>> GetServicesAsync(CancellationToken cancellationToken = default)
        => await db.ServiceCatalogs.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.Category).ThenBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<LabTest>> GetLabTestsAsync(CancellationToken cancellationToken = default)
        => await db.LabTests.AsNoTracking().Where(t => t.IsActive).OrderBy(t => t.Category).ThenBy(t => t.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ImagingService>> GetImagingServicesAsync(CancellationToken cancellationToken = default)
        => await db.ImagingServices.AsNoTracking().Where(i => i.IsActive).OrderBy(i => i.Modality).ThenBy(i => i.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Medicine>> GetMedicinesAsync(CancellationToken cancellationToken = default)
        => await db.Medicines.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Name).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Warehouse>> GetWarehousesAsync(Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        var q = db.Warehouses.AsNoTracking().Where(w => w.IsActive);
        if (facilityId.HasValue) q = q.Where(w => w.FacilityId == facilityId.Value);
        return await q.OrderBy(w => w.Code).ToListAsync(cancellationToken);
    }

    public async Task<OperationResult> UpdateHospitalAsync(Guid id, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken = default)
    {
        var entity = await db.Hospitals.FirstOrDefaultAsync(h => h.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy thông tin bệnh viện.");
        entity.Name = name.Trim();
        entity.TaxCode = taxCode?.Trim();
        entity.Phone = phone?.Trim();
        entity.Email = email?.Trim();
        entity.Address = address?.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("HospitalUpdated", "Hospital", entity.Id.ToString(), afterSummary: new { entity.Name, entity.TaxCode }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật thông tin bệnh viện.");
    }

    public async Task<OperationResult<Guid>> CreateFacilityAsync(Guid hospitalId, string code, string name, string? phone, string? address, bool isDefault, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Facilities.AnyAsync(f => f.HospitalId == hospitalId && f.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã cơ sở '{code}' đã tồn tại.");

        var entity = new Facility { HospitalId = hospitalId, Code = code, Name = name.Trim(), Phone = phone?.Trim(), Address = address?.Trim(), IsDefault = isDefault };
        db.Facilities.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("FacilityCreated", "Facility", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, facilityId: entity.Id, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm cơ sở thành công.");
    }

    public async Task<OperationResult> UpdateFacilityAsync(Guid id, string name, string? phone, string? address, bool isDefault, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Facilities.FirstOrDefaultAsync(f => f.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy cơ sở.");
        entity.Name = name.Trim();
        entity.Phone = phone?.Trim();
        entity.Address = address?.Trim();
        entity.IsDefault = isDefault;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("FacilityUpdated", "Facility", entity.Id.ToString(), afterSummary: new { entity.Name, entity.IsActive }, facilityId: entity.Id, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật cơ sở.");
    }

    public async Task<OperationResult<Guid>> CreateDepartmentAsync(Guid facilityId, string code, string name, DepartmentType type, string? location, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Departments.AnyAsync(d => d.FacilityId == facilityId && d.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã khoa/phòng '{code}' đã tồn tại trong cơ sở.");

        var entity = new Department { FacilityId = facilityId, Code = code, Name = name.Trim(), DepartmentType = type, Location = location?.Trim() };
        db.Departments.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("DepartmentCreated", "Department", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, facilityId: facilityId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm khoa/phòng thành công.");
    }

    public async Task<OperationResult> UpdateDepartmentAsync(Guid id, string name, DepartmentType type, string? phone, string? location, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy khoa/phòng.");
        entity.Name = name.Trim();
        entity.DepartmentType = type;
        entity.Phone = phone?.Trim();
        entity.Location = location?.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("DepartmentUpdated", "Department", entity.Id.ToString(), afterSummary: new { entity.Name, entity.IsActive }, facilityId: entity.FacilityId, departmentId: entity.Id, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật khoa/phòng.");
    }

    public async Task<OperationResult<Guid>> CreateSpecialtyAsync(string code, string name, string? description, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Specialties.AnyAsync(s => s.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã chuyên khoa '{code}' đã tồn tại.");

        var entity = new Specialty { Code = code, Name = name.Trim(), Description = description?.Trim() };
        db.Specialties.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("SpecialtyCreated", "Specialty", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm chuyên khoa.");
    }

    public async Task<OperationResult> UpdateSpecialtyAsync(Guid id, string name, string? description, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Specialties.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy chuyên khoa.");
        entity.Name = name.Trim();
        entity.Description = description?.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("SpecialtyUpdated", "Specialty", entity.Id.ToString(), afterSummary: new { entity.Name, entity.IsActive }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật chuyên khoa.");
    }

    public async Task<OperationResult<Guid>> CreateRoomAsync(Guid facilityId, Guid departmentId, string code, string name, RoomType roomType, string? floor, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Rooms.AnyAsync(r => r.FacilityId == facilityId && r.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã phòng '{code}' đã tồn tại.");

        var entity = new Room { FacilityId = facilityId, DepartmentId = departmentId, Code = code, Name = name.Trim(), RoomType = roomType, Floor = floor?.Trim() };
        db.Rooms.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("RoomCreated", "Room", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, facilityId: facilityId, departmentId: departmentId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm phòng thành công.");
    }

    public async Task<OperationResult> UpdateRoomAsync(Guid id, Guid departmentId, string name, RoomType roomType, string? floor, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Rooms.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy phòng.");
        entity.DepartmentId = departmentId;
        entity.Name = name.Trim();
        entity.RoomType = roomType;
        entity.Floor = floor?.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("RoomUpdated", "Room", entity.Id.ToString(), afterSummary: new { entity.Name, entity.IsActive }, facilityId: entity.FacilityId, departmentId: departmentId, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật thông tin phòng.");
    }

    public async Task<OperationResult<Guid>> CreateClinicAsync(Guid facilityId, Guid departmentId, Guid? specialtyId, Guid? roomId, string code, string name, string queuePrefix, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Clinics.AnyAsync(c => c.FacilityId == facilityId && c.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã phòng khám '{code}' đã tồn tại.");

        var entity = new Clinic
        {
            FacilityId = facilityId,
            DepartmentId = departmentId,
            SpecialtyId = specialtyId,
            RoomId = roomId,
            Code = code,
            Name = name.Trim(),
            QueuePrefix = string.IsNullOrWhiteSpace(queuePrefix) ? "K" : queuePrefix.Trim().ToUpperInvariant()
        };
        db.Clinics.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ClinicCreated", "Clinic", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, facilityId: facilityId, departmentId: departmentId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm phòng khám thành công.");
    }

    public async Task<OperationResult> UpdateClinicAsync(Guid id, Guid departmentId, Guid? specialtyId, Guid? roomId, string name, string queuePrefix, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Clinics.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy phòng khám.");
        entity.DepartmentId = departmentId;
        entity.SpecialtyId = specialtyId;
        entity.RoomId = roomId;
        entity.Name = name.Trim();
        entity.QueuePrefix = string.IsNullOrWhiteSpace(queuePrefix) ? "K" : queuePrefix.Trim().ToUpperInvariant();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ClinicUpdated", "Clinic", entity.Id.ToString(), afterSummary: new { entity.Name, entity.QueuePrefix, entity.IsActive }, facilityId: entity.FacilityId, departmentId: departmentId, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật phòng khám.");
    }

    public async Task<OperationResult<Guid>> CreateBedAsync(Guid facilityId, Guid departmentId, Guid roomId, string code, string name, decimal dailyRate, CancellationToken cancellationToken = default)
    {
        if (dailyRate < 0) return OperationResult<Guid>.Fail("Giá giường không hợp lệ.");
        code = code.Trim().ToUpperInvariant();
        if (await db.Beds.AnyAsync(b => b.FacilityId == facilityId && b.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã giường '{code}' đã tồn tại.");

        var entity = new Bed { FacilityId = facilityId, DepartmentId = departmentId, RoomId = roomId, Code = code, Name = name.Trim(), DailyRate = dailyRate };
        db.Beds.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("BedCreated", "Bed", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name, entity.DailyRate }, facilityId: facilityId, departmentId: departmentId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm giường bệnh thành công.");
    }

    public async Task<OperationResult> UpdateBedAsync(Guid id, Guid roomId, string name, decimal dailyRate, BedStatus status, bool isActive, CancellationToken cancellationToken = default)
    {
        if (dailyRate < 0) return OperationResult.Fail("Giá giường không hợp lệ.");
        var entity = await db.Beds.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy giường bệnh.");

        var hasActiveAssignment = await db.BedAssignments.AnyAsync(ba => ba.BedId == id && ba.IsCurrent, cancellationToken);
        if (hasActiveAssignment && (!isActive || status != BedStatus.Occupied))
            return OperationResult.Fail("Giường đang có bệnh nhân nằm, không thể ngừng hoạt động hoặc đổi trạng thái khỏi Occupied.");

        var room = await db.Rooms.AsNoTracking().FirstOrDefaultAsync(r => r.Id == roomId && r.FacilityId == entity.FacilityId, cancellationToken);
        if (room is null) return OperationResult.Fail("Phòng được chọn không hợp lệ.");

        entity.RoomId = room.Id;
        entity.DepartmentId = room.DepartmentId;
        entity.Name = name.Trim();
        entity.DailyRate = dailyRate;
        entity.Status = status;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("BedUpdated", "Bed", entity.Id.ToString(), afterSummary: new { entity.Name, entity.DailyRate, Status = status.ToString(), entity.IsActive }, facilityId: entity.FacilityId, departmentId: entity.DepartmentId, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật thông tin giường bệnh.");
    }

    public async Task<OperationResult<Guid>> CreateServiceAsync(string code, string name, string category, Guid? departmentId, decimal defaultPrice, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult<Guid>.Fail("Đơn giá không hợp lệ.");
        code = code.Trim().ToUpperInvariant();
        if (await db.ServiceCatalogs.AnyAsync(s => s.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã dịch vụ '{code}' đã tồn tại.");

        var entity = new ServiceCatalog { Code = code, Name = name.Trim(), Category = category.Trim(), DepartmentId = departmentId, DefaultPrice = defaultPrice };
        db.ServiceCatalogs.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ServiceCreated", "ServiceCatalog", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name, entity.DefaultPrice }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm dịch vụ thành công.");
    }

    public async Task<OperationResult> UpdateServiceAsync(Guid id, string name, string category, Guid? departmentId, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult.Fail("Đơn giá không hợp lệ.");
        var entity = await db.ServiceCatalogs.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy dịch vụ.");
        entity.Name = name.Trim();
        entity.Category = category.Trim();
        entity.DepartmentId = departmentId;
        entity.DefaultPrice = defaultPrice;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ServiceUpdated", "ServiceCatalog", entity.Id.ToString(), afterSummary: new { entity.Name, entity.DefaultPrice, entity.IsActive }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật dịch vụ.");
    }

    public async Task<OperationResult<Guid>> CreateLabTestAsync(string code, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult<Guid>.Fail("Đơn giá không hợp lệ.");
        code = code.Trim().ToUpperInvariant();
        if (await db.LabTests.AnyAsync(t => t.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã xét nghiệm '{code}' đã tồn tại.");

        var entity = new LabTest
        {
            Code = code, Name = name.Trim(), Category = category.Trim(), SpecimenType = specimenType.Trim(),
            Unit = unit?.Trim(), ReferenceRangeText = refText?.Trim(), ReferenceMin = refMin, ReferenceMax = refMax, DefaultPrice = defaultPrice
        };
        db.LabTests.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabTestCreated", "LabTest", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name, entity.DefaultPrice }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm danh mục xét nghiệm.");
    }

    public async Task<OperationResult> UpdateLabTestAsync(Guid id, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult.Fail("Đơn giá không hợp lệ.");
        var entity = await db.LabTests.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy xét nghiệm.");
        entity.Name = name.Trim();
        entity.Category = category.Trim();
        entity.SpecimenType = specimenType.Trim();
        entity.Unit = unit?.Trim();
        entity.ReferenceRangeText = refText?.Trim();
        entity.ReferenceMin = refMin;
        entity.ReferenceMax = refMax;
        entity.DefaultPrice = defaultPrice;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("LabTestUpdated", "LabTest", entity.Id.ToString(), afterSummary: new { entity.Name, entity.DefaultPrice, entity.IsActive }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật xét nghiệm.");
    }

    public async Task<OperationResult<Guid>> CreateImagingServiceAsync(string code, string name, string modality, decimal defaultPrice, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult<Guid>.Fail("Đơn giá không hợp lệ.");
        code = code.Trim().ToUpperInvariant();
        if (await db.ImagingServices.AnyAsync(i => i.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã dịch vụ CĐHA '{code}' đã tồn tại.");

        var entity = new ImagingService { Code = code, Name = name.Trim(), Modality = modality.Trim(), DefaultPrice = defaultPrice };
        db.ImagingServices.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ImagingServiceCreated", "ImagingService", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm dịch vụ CĐHA.");
    }

    public async Task<OperationResult> UpdateImagingServiceAsync(Guid id, string name, string modality, decimal defaultPrice, bool isActive, CancellationToken cancellationToken = default)
    {
        if (defaultPrice < 0) return OperationResult.Fail("Đơn giá không hợp lệ.");
        var entity = await db.ImagingServices.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy dịch vụ CĐHA.");
        entity.Name = name.Trim();
        entity.Modality = modality.Trim();
        entity.DefaultPrice = defaultPrice;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ImagingServiceUpdated", "ImagingService", entity.Id.ToString(), afterSummary: new { entity.Name, entity.DefaultPrice, entity.IsActive }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật dịch vụ CĐHA.");
    }

    public async Task<OperationResult<Guid>> CreateMedicineAsync(string code, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, CancellationToken cancellationToken = default)
    {
        if (defaultUnitPrice < 0) return OperationResult<Guid>.Fail("Đơn giá không hợp lệ.");
        code = code.Trim().ToUpperInvariant();
        if (await db.Medicines.AnyAsync(m => m.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã thuốc '{code}' đã tồn tại.");

        var entity = new Medicine
        {
            Code = code, Name = name.Trim(), ActiveIngredient = activeIngredient.Trim(), Strength = strength?.Trim(),
            DosageForm = dosageForm.Trim(), BaseUnit = baseUnit.Trim(), DefaultRoute = defaultRoute.Trim(),
            DefaultUnitPrice = defaultUnitPrice, MinStockLevel = minStockLevel, IsControlledSubstance = isControlledSubstance
        };
        db.Medicines.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("MedicineCreated", "Medicine", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm thuốc vào danh mục.");
    }

    public async Task<OperationResult> UpdateMedicineAsync(Guid id, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, bool isActive, CancellationToken cancellationToken = default)
    {
        if (defaultUnitPrice < 0) return OperationResult.Fail("Đơn giá không hợp lệ.");
        var entity = await db.Medicines.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy thuốc.");
        entity.Name = name.Trim();
        entity.ActiveIngredient = activeIngredient.Trim();
        entity.Strength = strength?.Trim();
        entity.DosageForm = dosageForm.Trim();
        entity.BaseUnit = baseUnit.Trim();
        entity.DefaultRoute = defaultRoute.Trim();
        entity.DefaultUnitPrice = defaultUnitPrice;
        entity.MinStockLevel = minStockLevel;
        entity.IsControlledSubstance = isControlledSubstance;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("MedicineUpdated", "Medicine", entity.Id.ToString(), afterSummary: new { entity.Name, entity.DefaultUnitPrice, entity.IsActive }, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã cập nhật danh mục thuốc.");
    }

    public async Task<OperationResult<Guid>> CreateWarehouseAsync(Guid facilityId, Guid? departmentId, string code, string name, string warehouseType, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Warehouses.AnyAsync(w => w.FacilityId == facilityId && w.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã kho '{code}' đã tồn tại.");
        var entity = new Warehouse { FacilityId = facilityId, DepartmentId = departmentId, Code = code, Name = name.Trim(), WarehouseType = warehouseType.Trim() };
        db.Warehouses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("WarehouseCreated", "Warehouse", entity.Id.ToString(), afterSummary: new { entity.Code, entity.Name }, facilityId: facilityId, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm kho.");
    }

    public async Task<OperationResult> UpdateWarehouseAsync(Guid id, string name, string warehouseType, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Warehouses.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy kho.");
        entity.Name = name.Trim();
        entity.WarehouseType = warehouseType.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã cập nhật thông tin kho.");
    }

    public async Task<OperationResult<Guid>> CreateSupplierAsync(string code, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.Suppliers.AnyAsync(s => s.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã nhà cung cấp '{code}' đã tồn tại.");
        var entity = new Supplier { Code = code, Name = name.Trim(), TaxCode = taxCode?.Trim(), Phone = phone?.Trim(), Email = email?.Trim(), Address = address?.Trim() };
        db.Suppliers.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm nhà cung cấp.");
    }

    public async Task<OperationResult> UpdateSupplierAsync(Guid id, string name, string? taxCode, string? phone, string? email, string? address, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Suppliers.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy nhà cung cấp.");
        entity.Name = name.Trim();
        entity.TaxCode = taxCode?.Trim();
        entity.Phone = phone?.Trim();
        entity.Email = email?.Trim();
        entity.Address = address?.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã cập nhật nhà cung cấp.");
    }

    public async Task<OperationResult<Guid>> CreateInventoryItemAsync(string code, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.InventoryItems.AnyAsync(i => i.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã vật tư '{code}' đã tồn tại.");
        var entity = new InventoryItem { Code = code, Name = name.Trim(), Category = category.Trim(), Unit = unit.Trim(), DefaultUnitPrice = defaultUnitPrice, MinStockLevel = minStockLevel, MaxStockLevel = maxStockLevel };
        db.InventoryItems.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm vật tư y tế.");
    }

    public async Task<OperationResult> UpdateInventoryItemAsync(Guid id, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.InventoryItems.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy vật tư.");
        entity.Name = name.Trim();
        entity.Category = category.Trim();
        entity.Unit = unit.Trim();
        entity.DefaultUnitPrice = defaultUnitPrice;
        entity.MinStockLevel = minStockLevel;
        entity.MaxStockLevel = maxStockLevel;
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã cập nhật vật tư y tế.");
    }

    public async Task<OperationResult<Guid>> CreateDiagnosisAsync(string icdCode, string name, string? chapter, CancellationToken cancellationToken = default)
    {
        icdCode = icdCode.Trim().ToUpperInvariant();
        if (await db.Diagnoses.AnyAsync(d => d.IcdCode == icdCode, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã ICD '{icdCode}' đã tồn tại.");
        var entity = new Diagnosis { IcdCode = icdCode, Name = name.Trim(), Chapter = chapter?.Trim() };
        db.Diagnoses.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã thêm mã bệnh ICD.");
    }

    public async Task<OperationResult> UpdateDiagnosisAsync(Guid id, string name, string? chapter, bool isActive, CancellationToken cancellationToken = default)
    {
        var entity = await db.Diagnoses.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
        if (entity is null) return OperationResult.Fail("Không tìm thấy mã bệnh ICD.");
        entity.Name = name.Trim();
        entity.Chapter = chapter?.Trim();
        entity.IsActive = isActive;
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult.Ok("Đã cập nhật mã bệnh ICD.");
    }

    public async Task<OperationResult<Guid>> CreatePriceListAsync(string code, string name, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isDefault, CancellationToken cancellationToken = default)
    {
        code = code.Trim().ToUpperInvariant();
        if (await db.ServicePriceLists.AnyAsync(p => p.Code == code, cancellationToken))
            return OperationResult<Guid>.Fail($"Mã bảng giá '{code}' đã tồn tại.");
        var entity = new ServicePriceList { Code = code, Name = name.Trim(), EffectiveFrom = effectiveFrom, EffectiveTo = effectiveTo, IsDefault = isDefault };
        db.ServicePriceLists.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Ok(entity.Id, "Đã tạo bảng giá mới.");
    }

    public async Task<OperationResult> UpsertServicePriceAsync(Guid priceListId, string itemType, Guid itemId, decimal unitPrice, CancellationToken cancellationToken = default)
    {
        if (unitPrice < 0) return OperationResult.Fail("Đơn giá không hợp lệ.");
        var priceList = await db.ServicePriceLists.AsNoTracking().FirstOrDefaultAsync(p => p.Id == priceListId, cancellationToken);
        if (priceList is null) return OperationResult.Fail("Không tìm thấy bảng giá.");

        string itemCode = "", itemName = "";
        if (itemType == "Service")
        {
            var s = await db.ServiceCatalogs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (s is null) return OperationResult.Fail("Không tìm thấy dịch vụ.");
            itemCode = s.Code; itemName = s.Name;
        }
        else if (itemType == "LabTest")
        {
            var t = await db.LabTests.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (t is null) return OperationResult.Fail("Không tìm thấy xét nghiệm.");
            itemCode = t.Code; itemName = t.Name;
        }
        else if (itemType == "Imaging")
        {
            var im = await db.ImagingServices.AsNoTracking().FirstOrDefaultAsync(x => x.Id == itemId, cancellationToken);
            if (im is null) return OperationResult.Fail("Không tìm thấy dịch vụ CĐHA.");
            itemCode = im.Code; itemName = im.Name;
        }

        var existing = await db.ServicePrices.FirstOrDefaultAsync(
            sp => sp.ServicePriceListId == priceListId && sp.ItemType == itemType && sp.ItemId == itemId, cancellationToken);
        if (existing is null)
        {
            db.ServicePrices.Add(new ServicePrice
            {
                ServicePriceListId = priceListId, ItemType = itemType, ItemId = itemId,
                ItemCode = itemCode, ItemName = itemName, UnitPrice = unitPrice
            });
        }
        else
        {
            existing.UnitPrice = unitPrice;
            existing.IsActive = true;
        }

        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("ServicePriceUpserted", "ServicePriceList", priceListId.ToString(),
            afterSummary: new { itemType, itemCode, unitPrice }, cancellationToken: cancellationToken);
        return OperationResult.Ok($"Đã cập nhật giá {itemName}: {unitPrice:N0} đ.");
    }
}
