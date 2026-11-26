using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Admin;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.MasterData;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class UsersController(
    IUserAdminService userAdminService,
    IRoleAdminService roleAdminService,
    IMasterDataService masterDataService) : Controller
{
    [HasPermission(PermissionCodes.AdminUserManage)]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await userAdminService.GetUsersAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, cancellationToken);
        ViewBag.Search = search;
        return View(result);
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        ViewBag.Roles = await roleAdminService.GetRolesAsync(cancellationToken);
        ViewBag.Facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(null, cancellationToken);
        return View();
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpPost]
    public async Task<IActionResult> Create(
        string userName, string fullName, string? email, string password, string? employeeCode,
        Guid? defaultFacilityId, Guid? defaultDepartmentId, bool mustChangePassword,
        string[] roles, Guid[] facilityScopeIds, Guid[] departmentScopeIds, CancellationToken cancellationToken)
    {
        var res = await userAdminService.CreateUserAsync(new CreateUserRequest(
            userName, fullName, email, password, employeeCode, defaultFacilityId, defaultDepartmentId,
            mustChangePassword, roles ?? [], facilityScopeIds ?? [], departmentScopeIds ?? []), cancellationToken);

        if (!res.Succeeded)
        {
            TempData["Error"] = res.Message;
            return RedirectToAction(nameof(Create));
        }

        TempData["Success"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var user = await userAdminService.GetUserByIdAsync(id, cancellationToken);
        if (user is null) return NotFound();

        var scopes = await userAdminService.GetUserScopesAsync(id, cancellationToken);
        ViewBag.Roles = await roleAdminService.GetRolesAsync(cancellationToken);
        ViewBag.Facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
        ViewBag.Departments = await masterDataService.GetDepartmentsAsync(null, cancellationToken);
        ViewBag.FacilityScopeIds = scopes.FacilityIds;
        ViewBag.DepartmentScopeIds = scopes.DepartmentIds;
        return View(user);
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpPost]
    public async Task<IActionResult> Edit(
        Guid id, string fullName, string? email, string? employeeCode, bool isActive,
        Guid? defaultFacilityId, Guid? defaultDepartmentId,
        string[] roles, Guid[] facilityScopeIds, Guid[] departmentScopeIds, CancellationToken cancellationToken)
    {
        var res = await userAdminService.UpdateUserAsync(new UpdateUserRequest(
            id, fullName, email, employeeCode, isActive, defaultFacilityId, defaultDepartmentId,
            roles ?? [], facilityScopeIds ?? [], departmentScopeIds ?? []), cancellationToken);

        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpPost]
    public async Task<IActionResult> ToggleActive(Guid id, bool isActive, string reason, CancellationToken cancellationToken)
    {
        var res = await userAdminService.ToggleUserActiveAsync(id, isActive, reason ?? "Quản trị viên thay đổi trạng thái", cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.AdminUserManage)]
    [HttpPost]
    public async Task<IActionResult> ResetPassword(Guid id, string newPassword, bool mustChangePassword, string reason, CancellationToken cancellationToken)
    {
        var res = await userAdminService.ResetPasswordAsync(id, newPassword, mustChangePassword, reason ?? "Đặt lại mật khẩu bởi quản trị viên", cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Edit), new { id });
    }
}

[Area("Admin")]
[Authorize]
public class RolesController(IRoleAdminService roleAdminService) : Controller
{
    [HasPermission(PermissionCodes.AdminRoleManage)]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var roles = await roleAdminService.GetRolesAsync(cancellationToken);
        ViewBag.AllPermissions = await roleAdminService.GetAllPermissionsAsync(cancellationToken);
        return View(roles);
    }

    [HasPermission(PermissionCodes.AdminRoleManage)]
    [HttpPost]
    public async Task<IActionResult> UpdatePermissions(Guid roleId, string[] permissionCodes, CancellationToken cancellationToken)
    {
        var res = await roleAdminService.UpdateRolePermissionsAsync(roleId, permissionCodes ?? [], cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.AdminRoleManage)]
    [HttpPost]
    public async Task<IActionResult> CreateRole(string name, string displayName, string? description, CancellationToken cancellationToken)
    {
        var res = await roleAdminService.CreateRoleAsync(name, displayName, description, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }
}

[Area("Admin")]
[Authorize]
public class CatalogsController(IMasterDataService masterDataService, ICurrentUserService currentUser) : Controller
{
    [HasPermission(PermissionCodes.AdminCatalogManage)]
    public async Task<IActionResult> Index(string? tab = null, CancellationToken cancellationToken = default)
    {
        var snapshot = await masterDataService.GetCatalogSnapshotAsync(currentUser.ActiveFacilityId, includeInactive: true, cancellationToken);
        ViewBag.ActiveTab = string.IsNullOrWhiteSpace(tab) ? "org" : tab;
        return View(snapshot);
    }

    private IActionResult RedirectTab(string tab) => RedirectToAction(nameof(Index), new { tab });

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateHospital(Guid id, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateHospitalAsync(id, name, taxCode, phone, email, address, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateFacility(Guid hospitalId, string code, string name, string? phone, string? address, bool isDefault, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateFacilityAsync(hospitalId, code, name, phone, address, isDefault, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateFacility(Guid id, string name, string? phone, string? address, bool isDefault, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateFacilityAsync(id, name, phone, address, isDefault, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateDepartment(Guid facilityId, string code, string name, DepartmentType departmentType, string? location, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateDepartmentAsync(facilityId, code, name, departmentType, location, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateDepartment(Guid id, string name, DepartmentType departmentType, string? phone, string? location, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateDepartmentAsync(id, name, departmentType, phone, location, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateSpecialty(string code, string name, string? description, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateSpecialtyAsync(code, name, description, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateSpecialty(Guid id, string name, string? description, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateSpecialtyAsync(id, name, description, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("org");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateRoom(Guid facilityId, Guid departmentId, string code, string name, RoomType roomType, string? floor, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateRoomAsync(facilityId, departmentId, code, name, roomType, floor, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateRoom(Guid id, Guid departmentId, string name, RoomType roomType, string? floor, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateRoomAsync(id, departmentId, name, roomType, floor, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateClinic(Guid facilityId, Guid departmentId, Guid? specialtyId, Guid? roomId, string code, string name, string queuePrefix, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateClinicAsync(facilityId, departmentId, specialtyId, roomId, code, name, queuePrefix, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateClinic(Guid id, Guid departmentId, Guid? specialtyId, Guid? roomId, string name, string queuePrefix, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateClinicAsync(id, departmentId, specialtyId, roomId, name, queuePrefix, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateBed(Guid facilityId, Guid roomId, string code, string name, decimal dailyRate, CancellationToken cancellationToken)
    {
        var room = (await masterDataService.GetRoomsAsync(facilityId, cancellationToken)).FirstOrDefault(r => r.Id == roomId);
        if (room is null)
        {
            TempData["Error"] = "Phòng được chọn không hợp lệ.";
            return RedirectTab("rooms");
        }
        var res = await masterDataService.CreateBedAsync(facilityId, room.DepartmentId, room.Id, code, name, dailyRate, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateBed(Guid id, Guid roomId, string name, decimal dailyRate, BedStatus status, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateBedAsync(id, roomId, name, dailyRate, status, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("rooms");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateService(string code, string name, string category, Guid? departmentId, decimal defaultPrice, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateServiceAsync(code, name, category, departmentId, defaultPrice, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateService(Guid id, string name, string category, Guid? departmentId, decimal defaultPrice, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateServiceAsync(id, name, category, departmentId, defaultPrice, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateLabTest(string code, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateLabTestAsync(code, name, category, specimenType, unit, refText, refMin, refMax, defaultPrice, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("clinical");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateLabTest(Guid id, string name, string category, string specimenType, string? unit, string? refText, decimal? refMin, decimal? refMax, decimal defaultPrice, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateLabTestAsync(id, name, category, specimenType, unit, refText, refMin, refMax, defaultPrice, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("clinical");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateImagingService(string code, string name, string modality, decimal defaultPrice, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateImagingServiceAsync(code, name, modality, defaultPrice, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("clinical");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateImagingService(Guid id, string name, string modality, decimal defaultPrice, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateImagingServiceAsync(id, name, modality, defaultPrice, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("clinical");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateMedicine(string code, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateMedicineAsync(code, name, activeIngredient, strength, dosageForm, baseUnit, defaultRoute, defaultUnitPrice, minStockLevel, isControlledSubstance, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("pharmacy");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateMedicine(Guid id, string name, string activeIngredient, string? strength, string dosageForm, string baseUnit, string defaultRoute, decimal defaultUnitPrice, decimal minStockLevel, bool isControlledSubstance, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateMedicineAsync(id, name, activeIngredient, strength, dosageForm, baseUnit, defaultRoute, defaultUnitPrice, minStockLevel, isControlledSubstance, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("pharmacy");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateWarehouse(Guid facilityId, Guid? departmentId, string code, string name, string warehouseType, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateWarehouseAsync(facilityId, departmentId, code, name, warehouseType, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateWarehouse(Guid id, string name, string warehouseType, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateWarehouseAsync(id, name, warehouseType, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateSupplier(string code, string name, string? taxCode, string? phone, string? email, string? address, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateSupplierAsync(code, name, taxCode, phone, email, address, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateSupplier(Guid id, string name, string? taxCode, string? phone, string? email, string? address, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateSupplierAsync(id, name, taxCode, phone, email, address, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateInventoryItem(string code, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateInventoryItemAsync(code, name, category, unit, defaultUnitPrice, minStockLevel, maxStockLevel, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateInventoryItem(Guid id, string name, string category, string unit, decimal defaultUnitPrice, decimal minStockLevel, decimal maxStockLevel, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateInventoryItemAsync(id, name, category, unit, defaultUnitPrice, minStockLevel, maxStockLevel, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("supply");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> CreateDiagnosis(string icdCode, string name, string? chapter, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreateDiagnosisAsync(icdCode, name, chapter, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }

    [HasPermission(PermissionCodes.AdminCatalogManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateDiagnosis(Guid id, string name, string? chapter, bool isActive, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpdateDiagnosisAsync(id, name, chapter, isActive, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }

    [HasPermission(PermissionCodes.BillingPriceManage)]
    [HttpPost]
    public async Task<IActionResult> CreatePriceList(string code, string name, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isDefault, CancellationToken cancellationToken)
    {
        var res = await masterDataService.CreatePriceListAsync(code, name, effectiveFrom, effectiveTo, isDefault, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }

    [HasPermission(PermissionCodes.BillingPriceManage)]
    [HttpPost]
    public async Task<IActionResult> UpsertServicePrice(Guid priceListId, string itemType, Guid itemId, decimal unitPrice, CancellationToken cancellationToken)
    {
        var res = await masterDataService.UpsertServicePriceAsync(priceListId, itemType, itemId, unitPrice, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectTab("services");
    }
}

[Area("Admin")]
[Authorize]
public class SystemController(
    IAuditQueryService auditQueryService,
    IHospitalDbContext db,
    IHostEnvironment environment,
    IAuditService auditService) : Controller
{
    [HasPermission(PermissionCodes.AuditLogRead)]
    public async Task<IActionResult> AuditLogs(string? search, string? entityType, int page = 1, CancellationToken cancellationToken = default)
    {
        var logs = await auditQueryService.GetAuditLogsAsync(new PagedRequest { Search = search, Page = page, PageSize = 25 }, entityType, null, null, null, cancellationToken);
        ViewBag.Search = search;
        ViewBag.EntityType = entityType;
        return View(logs);
    }

    [HasPermission(PermissionCodes.IntegrationRead)]
    public async Task<IActionResult> Integrations(string? search, string? integrationType, int page = 1, CancellationToken cancellationToken = default)
    {
        var msgs = await auditQueryService.GetIntegrationMessagesAsync(new PagedRequest { Search = search, Page = page, PageSize = 25 }, integrationType, cancellationToken);
        ViewBag.Search = search;
        return View(msgs);
    }

    [HasPermission(PermissionCodes.AdminSettingsManage)]
    public async Task<IActionResult> Settings(CancellationToken cancellationToken)
    {
        var settings = await db.SystemSettings.AsNoTracking().OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync(cancellationToken);
        return View(settings);
    }

    [HasPermission(PermissionCodes.AdminSettingsManage)]
    [HttpPost]
    public async Task<IActionResult> UpdateSetting(Guid id, string value, CancellationToken cancellationToken)
    {
        var setting = await db.SystemSettings.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (setting is null) return NotFound();
        var oldVal = setting.Value;
        setting.Value = value.Trim();
        await db.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync("SystemSettingUpdated", "SystemSetting", setting.Key,
            beforeSummary: new { Value = oldVal }, afterSummary: new { setting.Value }, cancellationToken: cancellationToken);
        TempData["Success"] = $"Đã cập nhật cấu hình {setting.Key}.";
        return RedirectToAction(nameof(Settings));
    }

    [HasPermission(PermissionCodes.AdminSystemStatusRead)]
    public async Task<IActionResult> Status(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        var pendingMigrations = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();
        var appliedMigrations = (await db.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();

        ViewBag.EnvironmentName = environment.EnvironmentName;
        ViewBag.CanConnect = canConnect;
        ViewBag.DatabaseProvider = db.Database.ProviderName;
        ViewBag.PendingMigrations = pendingMigrations;
        ViewBag.AppliedMigrations = appliedMigrations;
        ViewBag.PatientCount = await db.Patients.CountAsync(cancellationToken);
        ViewBag.EncounterCount = await db.Encounters.CountAsync(cancellationToken);
        return View();
    }
}
