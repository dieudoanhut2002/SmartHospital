using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Pharmacy;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Areas.Pharmacy.Controllers;

[Area("Pharmacy")]
[Authorize]
public class PharmacyController(
    IPharmacyAndInventoryService pharmacyService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser) : Controller
{
    [HasPermission(PermissionCodes.PharmacyRead)]
    public async Task<IActionResult> Index(string? search, PrescriptionStatus? status, int page = 1, CancellationToken cancellationToken = default)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id;
        var prescriptions = await pharmacyService.GetPrescriptionWorklistAsync(new PagedRequest { Search = search, Page = page, PageSize = 20 }, facId, status, cancellationToken);
        var warehouses = await masterDataService.GetWarehousesAsync(facId, cancellationToken);
        var pharmWh = warehouses.FirstOrDefault(w => w.WarehouseType == "Pharmacy") ?? warehouses.FirstOrDefault();

        ViewBag.Search = search;
        ViewBag.Warehouses = warehouses;
        ViewBag.Stock = await pharmacyService.GetMedicineStockAsync(pharmWh?.Id, false, cancellationToken);
        ViewBag.Medicines = await masterDataService.GetMedicinesAsync(cancellationToken);
        ViewBag.ActiveFacilityId = facId;
        return View(prescriptions);
    }

    [HasPermission(PermissionCodes.PharmacyRead)]
    public async Task<IActionResult> Dispense(Guid id, CancellationToken cancellationToken)
    {
        var prescription = await pharmacyService.GetPrescriptionDetailAsync(id, cancellationToken);
        if (prescription is null) return NotFound();
        var warehouses = await masterDataService.GetWarehousesAsync(prescription.FacilityId, cancellationToken);
        ViewBag.Warehouses = warehouses.Where(w => w.WarehouseType == "Pharmacy").ToList();
        return View(prescription);
    }

    [HasPermission(PermissionCodes.PharmacyDispense)]
    [HttpPost]
    public async Task<IActionResult> Dispense(Guid prescriptionId, Guid warehouseId, Guid[] prescriptionItemId, decimal[] quantityToDispense, CancellationToken cancellationToken)
    {
        var inputs = new List<DispenseItemInput>();
        for (int i = 0; i < (prescriptionItemId?.Length ?? 0); i++)
        {
            inputs.Add(new DispenseItemInput(prescriptionItemId![i], quantityToDispense.ElementAtOrDefault(i)));
        }

        var res = await pharmacyService.DispensePrescriptionAsync(prescriptionId, warehouseId, inputs, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Dispense), new { id = prescriptionId });
    }

    [HasPermission(PermissionCodes.PharmacyStockAdjust)]
    [HttpPost]
    public async Task<IActionResult> ReceiveBatch(Guid facilityId, Guid warehouseId, Guid medicineId, string batchNumber, DateOnly expiryDate, decimal quantity, decimal unitCost, string? notes, CancellationToken cancellationToken)
    {
        var res = await pharmacyService.ReceiveMedicineBatchAsync(facilityId, warehouseId, medicineId, batchNumber, expiryDate, quantity, unitCost, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.PharmacyStockAdjust)]
    [HttpPost]
    public async Task<IActionResult> AdjustStock(Guid facilityId, Guid warehouseId, Guid batchId, decimal deltaQuantity, string reason, CancellationToken cancellationToken)
    {
        var res = await pharmacyService.AdjustMedicineStockAsync(facilityId, warehouseId, batchId, deltaQuantity, reason, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Index));
    }

    [HasPermission(PermissionCodes.InventoryManage)]
    public async Task<IActionResult> Inventory(CancellationToken cancellationToken)
    {
        var facId = currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        ViewBag.Transactions = await pharmacyService.GetStockTransactionsAsync(null, 50, cancellationToken);
        ViewBag.InventoryItems = await pharmacyService.GetInventoryItemsAsync(cancellationToken);
        ViewBag.PurchaseRequests = await pharmacyService.GetPurchaseRequestsAsync(facId, cancellationToken);
        ViewBag.Warehouses = await masterDataService.GetWarehousesAsync(facId, cancellationToken);
        ViewBag.ActiveFacilityId = facId;
        return View();
    }

    [HasPermission(PermissionCodes.InventoryManage)]
    [HttpPost]
    public async Task<IActionResult> CreatePurchaseRequest(Guid facilityId, Guid warehouseId, string itemsSummary, decimal estimatedTotal, string? notes, CancellationToken cancellationToken)
    {
        var res = await pharmacyService.CreatePurchaseRequestAsync(facilityId, warehouseId, null, itemsSummary, estimatedTotal, notes, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Inventory));
    }

    [HasPermission(PermissionCodes.InventoryManage)]
    [HttpPost]
    public async Task<IActionResult> UpdatePurchaseRequestStatus(Guid purchaseRequestId, PurchaseRequestStatus status, CancellationToken cancellationToken)
    {
        var res = await pharmacyService.UpdatePurchaseRequestStatusAsync(purchaseRequestId, status, cancellationToken);
        TempData[res.Succeeded ? "Success" : "Error"] = res.Message;
        return RedirectToAction(nameof(Inventory));
    }
}
