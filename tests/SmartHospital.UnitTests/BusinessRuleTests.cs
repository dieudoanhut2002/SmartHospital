using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SmartHospital.Application.Billing;
using SmartHospital.Application.Clinical;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.Laboratory;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Pharmacy;
using SmartHospital.Application.Reception;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Pharmacy;
using SmartHospital.Infrastructure.Persistence;

namespace SmartHospital.UnitTests;

internal sealed class FakeCurrentUserService : ICurrentUserService
{
    public Guid? UserId => Guid.Parse("11111111-1111-1111-1111-111111111111");
    public string UserName => "tester";
    public string FullName => "Test Clinician";
    public bool IsAuthenticated => true;
    public Guid? ActiveFacilityId => null;
    public Guid? ActiveDepartmentId => null;
    public string? CorrelationId => "test-corr";
    public string? IpAddress => "127.0.0.1";
    public string? UserAgent => "xUnit";
    public IReadOnlyCollection<string> Roles => [RoleNames.SystemAdmin];
    public bool HasPermission(string permissionCode) => true;
    public bool IsInRole(string roleName) => Roles.Contains(roleName);
}

internal sealed class FakeAuditService : IAuditService
{
    public List<string> Actions { get; } = [];
    public Task LogAsync(string action, string entityType, string entityId, string? reason = null, object? beforeSummary = null, object? afterSummary = null, Guid? facilityId = null, Guid? departmentId = null, CancellationToken cancellationToken = default)
    {
        Actions.Add($"{action}:{entityType}:{entityId}");
        return Task.CompletedTask;
    }
}

internal sealed class FakeCodeGenerator : ICodeGeneratorService
{
    private int _seq;
    public Task<string> GenerateNextCodeAsync(string prefix, string sequenceKey, CancellationToken cancellationToken = default)
        => Task.FromResult($"{prefix}-{Interlocked.Increment(ref _seq):D5}");
}

internal sealed class FakeNotificationProvider : INotificationProvider
{
    public string ProviderName => "Fake";
    public Task<OperationResult> SendNotificationAsync(Guid? recipientUserId, Guid? patientId, string channel, string title, string message, string? linkUrl = null, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult.Ok());
}

internal sealed class FakeLabIntegration : ILabDeviceIntegration
{
    public string ProviderName => "FakeLab";
    public Task<OperationResult<string>> SendOrderToAnalyzerDemoAsync(Guid labOrderId, string orderCode, string idempotencyKey, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("DEMO"));
}

internal sealed class FakeRadIntegration : IRadiologyIntegration
{
    public string ProviderName => "FakeRad";
    public Task<OperationResult<string>> SendModalityWorklistDemoAsync(Guid imagingOrderId, string orderCode, string modality, string idempotencyKey, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("DEMO"));
}

internal sealed class FakePacsIntegration : IPacsIntegration
{
    public string ProviderName => "FakePacs";
    public Task<OperationResult<string>> GetDemoViewerUrlAsync(string orderCode, string modality, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("/demo"));
}

internal sealed class FakeScopeAuth : IScopeAuthorizationService
{
    public Task<bool> HasHospitalWideScopeAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<IReadOnlyList<Guid>> GetAllowedFacilityIdsAsync(Guid userId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
    public Task<IReadOnlyList<Guid>> GetAllowedDepartmentIdsAsync(Guid userId, Guid? facilityId = null, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Guid>>([]);
    public Task<bool> CanAccessFacilityAsync(Guid userId, Guid facilityId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> CanAccessDepartmentAsync(Guid userId, Guid facilityId, Guid departmentId, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<bool> CanAccessWarehouseAsync(Guid userId, Guid warehouseId, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

internal sealed class FakeBillingAdapters : IEInvoiceIntegration, IPaymentGateway, IInsuranceIntegration
{
    public string ProviderName => "FakeBilling";
    public Task<OperationResult<string>> IssueDraftEInvoiceDemoAsync(Guid invoiceId, string invoiceCode, decimal totalAmount, string idempotencyKey, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("HDĐT-TEST"));
    public Task<OperationResult<string>> RecordSandboxTransactionAsync(string receiptNumber, decimal amount, string method, string idempotencyKey, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("TXN-TEST"));
    public Task<OperationResult<string>> SubmitClaimStagingDemoAsync(Guid claimId, string claimCode, decimal claimedAmount, string idempotencyKey, CancellationToken cancellationToken = default)
        => Task.FromResult(OperationResult<string>.Ok("BHYT-TEST"));
}

internal sealed class FakeDocStorage : IDocumentStorage
{
    public Task<StoredFileResult> SaveAsync(Stream content, string originalFileName, string contentType, long sizeBytes, CancellationToken cancellationToken = default)
        => Task.FromResult(new StoredFileResult("key", "abc", sizeBytes));
    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream?>(null);
    public Task<bool> DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(true);
}

public class BusinessRuleTests
{
    private static HospitalDbContext CreateInMemoryDb()
    {
        var opts = new DbContextOptionsBuilder<HospitalDbContext>()
            .UseInMemoryDatabase($"sh_unit_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new HospitalDbContext(opts);
    }

    [Fact]
    public async Task PatientDuplicateDetection_BlocksDuplicateUnlessConfirmed()
    {
        await using var db = CreateInMemoryDb();
        var svc = new PatientService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());

        var first = await svc.CreatePatientAsync(new CreateOrUpdatePatientRequest(
            "Nguyễn Văn An", new DateOnly(1990, 1, 1), Gender.Male, "0901234567", null, "001090000111", null, null, false, null, null, null));
        Assert.True(first.Succeeded);

        var dup = await svc.CreatePatientAsync(new CreateOrUpdatePatientRequest(
            "Nguyễn Văn An", new DateOnly(1990, 1, 1), Gender.Male, "0909999999", null, null, null, null, false, null, null, null));
        Assert.False(dup.Succeeded);
        Assert.Contains("Cảnh báo trùng lặp", dup.Message);

        var forced = await svc.CreatePatientAsync(new CreateOrUpdatePatientRequest(
            "Nguyễn Văn An", new DateOnly(1990, 1, 1), Gender.Male, "0909999999", null, null, null, null, false, null, null, null, ConfirmDuplicateOverride: true));
        Assert.True(forced.Succeeded);
    }

    [Fact]
    public async Task QueueTicket_AssignsSequentialUniqueNumbersPerClinicAndDate()
    {
        await using var db = CreateInMemoryDb();
        var fac = new Facility { Code = "CS1", Name = "CS1" };
        var dept = new Department { FacilityId = fac.Id, Code = "OPD", Name = "OPD", DepartmentType = DepartmentType.Outpatient };
        var clinic = new Clinic { FacilityId = fac.Id, DepartmentId = dept.Id, Code = "PK1", Name = "PK1", QueuePrefix = "N1" };
        db.Facilities.Add(fac);
        db.Departments.Add(dept);
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync();

        var patientSvc = new PatientService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());
        var p1 = await patientSvc.CreatePatientAsync(new CreateOrUpdatePatientRequest("Bệnh nhân 1", null, Gender.Male, null, null, null, null, null, false, null, null, null));
        var p2 = await patientSvc.CreatePatientAsync(new CreateOrUpdatePatientRequest("Bệnh nhân 2", null, Gender.Female, null, null, null, null, null, false, null, null, null));

        var recSvc = new ReceptionService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService(), new FakeNotificationProvider());
        var r1 = await recSvc.CreateEncounterWithQueueAsync(new WalkInOrCheckInRequest(p1.Data, fac.Id, dept.Id, clinic.Id, null, null, null, EncounterType.Outpatient, "Sốt"));
        var r2 = await recSvc.CreateEncounterWithQueueAsync(new WalkInOrCheckInRequest(p2.Data, fac.Id, dept.Id, clinic.Id, null, null, null, EncounterType.Outpatient, "Ho"));

        Assert.True(r1.Succeeded);
        Assert.True(r2.Succeeded);
        var tickets = await db.QueueTickets.OrderBy(t => t.SequenceNumber).ToListAsync();
        Assert.Equal(2, tickets.Count);
        Assert.Equal("N1-001", tickets[0].TicketNumber);
        Assert.Equal("N1-002", tickets[1].TicketNumber);
    }

    [Fact]
    public async Task Appointment_CancelledCannotCheckIn()
    {
        await using var db = CreateInMemoryDb();
        var fac = new Facility { Code = "CS1", Name = "CS1" };
        var dept = new Department { FacilityId = fac.Id, Code = "OPD", Name = "OPD" };
        var clinic = new Clinic { FacilityId = fac.Id, DepartmentId = dept.Id, Code = "PK1", Name = "PK1", QueuePrefix = "K" };
        db.Facilities.Add(fac); db.Departments.Add(dept); db.Clinics.Add(clinic);
        await db.SaveChangesAsync();

        var patientSvc = new PatientService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());
        var p = await patientSvc.CreatePatientAsync(new CreateOrUpdatePatientRequest("BN Test", null, Gender.Male, null, null, null, null, null, false, null, null, null));

        var recSvc = new ReceptionService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService(), new FakeNotificationProvider());
        var appt = await recSvc.CreateAppointmentAsync(new CreateAppointmentRequest(p.Data, fac.Id, dept.Id, null, clinic.Id, null, DateTimeOffset.UtcNow.AddHours(2), "Khám", null));
        await recSvc.UpdateAppointmentStatusAsync(appt.Data, AppointmentStatus.Cancelled, "Bệnh nhân bận");

        var checkIn = await recSvc.CreateEncounterWithQueueAsync(new WalkInOrCheckInRequest(p.Data, fac.Id, dept.Id, clinic.Id, null, null, appt.Data, EncounterType.Outpatient, "Khám"));
        Assert.False(checkIn.Succeeded);
    }

    [Fact]
    public async Task BedAssignment_RejectsOccupiedBed()
    {
        await using var db = CreateInMemoryDb();
        var fac = new Facility { Code = "CS1", Name = "CS1" };
        var dept = new Department { FacilityId = fac.Id, Code = "NOI", Name = "Nội" };
        var room = new Room { FacilityId = fac.Id, DepartmentId = dept.Id, Code = "P401", Name = "P401" };
        var bed = new Bed { FacilityId = fac.Id, DepartmentId = dept.Id, RoomId = room.Id, Code = "G01", Name = "Giường 1", Status = BedStatus.Available };
        db.Facilities.Add(fac); db.Departments.Add(dept); db.Rooms.Add(room); db.Beds.Add(bed);
        await db.SaveChangesAsync();

        var ops = new InpatientAndHospitalOpsService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());
        var adm1 = await ops.AdmitPatientAsync(Guid.NewGuid(), Guid.NewGuid(), fac.Id, dept.Id, bed.Id, "Viêm phổi");
        Assert.True(adm1.Succeeded);

        var adm2 = await ops.AdmitPatientAsync(Guid.NewGuid(), Guid.NewGuid(), fac.Id, dept.Id, bed.Id, "Sốt");
        Assert.False(adm2.Succeeded);
    }

    [Fact]
    public async Task LabResult_CannotOverwriteVerifiedDirectly_MustAmendWithVersion()
    {
        await using var db = CreateInMemoryDb();
        var test = new LabTest { Code = "GLU", Name = "Glucose", ReferenceMin = 3.9m, ReferenceMax = 6.4m, Unit = "mmol/L" };
        var order = new LabOrder { OrderCode = "XN-1", EncounterId = Guid.NewGuid(), PatientId = Guid.NewGuid(), FacilityId = Guid.NewGuid(), DepartmentId = Guid.NewGuid() };
        var item = new LabOrderItem { LabOrderId = order.Id, LabTestId = test.Id, TestCode = test.Code, TestName = test.Name };
        order.Items.Add(item);
        db.LabTests.Add(test);
        db.LabOrders.Add(order);
        await db.SaveChangesAsync();

        var svc = new LabAndImagingService(db, new FakeCurrentUserService(), new FakeAuditService(), new FakeDocStorage(), new FakePacsIntegration(), new FakeNotificationProvider());
        await svc.EnterLabResultAsync(item.Id, "7.8", 7.8m, null);
        var resEntity = await db.LabResults.FirstAsync();
        Assert.True(resEntity.IsAbnormal);

        var verifyRes = await svc.VerifyLabResultAsync(resEntity.Id);
        Assert.True(verifyRes.Succeeded);

        var directOverwrite = await svc.EnterLabResultAsync(item.Id, "5.0", 5.0m, null);
        Assert.False(directOverwrite.Succeeded);

        var amendRes = await svc.AmendLabResultAsync(resEntity.Id, "5.2", 5.2m, "Hiệu chỉnh mẫu chuẩn");
        Assert.True(amendRes.Succeeded);
        Assert.Equal(2, resEntity.Version);
        Assert.False(resEntity.IsAbnormal);
        Assert.Equal(2, await db.LabResultVersions.CountAsync());
    }

    [Fact]
    public async Task PharmacyDispense_EnforcesAvailableStockAndPreventsOverDispensing()
    {
        await using var db = CreateInMemoryDb();
        var facId = Guid.NewGuid();
        var wh = new Warehouse { FacilityId = facId, Code = "KHO-DUOC", Name = "Kho Dược" };
        var med = new Medicine { Code = "PARA", Name = "Paracetamol", BaseUnit = "Viên", DefaultUnitPrice = 1500m };
        db.Warehouses.Add(wh);
        db.Medicines.Add(med);
        await db.SaveChangesAsync();

        var pharm = new PharmacyAndInventoryService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());
        await pharm.ReceiveMedicineBatchAsync(facId, wh.Id, med.Id, "LOT1", HospitalTime.TodayVietnam().AddMonths(6), 10m, 1000m, "Nhập đầu");

        var rx = new Domain.Clinical.Prescription
        {
            PrescriptionCode = "DT-1",
            FacilityId = facId,
            EncounterId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            DepartmentId = Guid.NewGuid(),
            Items = [new Domain.Clinical.PrescriptionItem { MedicineId = med.Id, MedicineCode = med.Code, MedicineName = med.Name, UnitName = "Viên", QuantityPrescribed = 15m, UnitPriceSnapshot = 1500m }]
        };
        db.Prescriptions.Add(rx);
        await db.SaveChangesAsync();

        var rxItem = rx.Items.First();
        var failOverStock = await pharm.DispensePrescriptionAsync(rx.Id, wh.Id, [new DispenseItemInput(rxItem.Id, 12m)]);
        Assert.False(failOverStock.Succeeded);

        var okDispense = await pharm.DispensePrescriptionAsync(rx.Id, wh.Id, [new DispenseItemInput(rxItem.Id, 8m)]);
        Assert.True(okDispense.Succeeded);

        var batch = await db.MedicineBatches.FirstAsync();
        Assert.Equal(2m, batch.QuantityOnHand);
        Assert.Equal(PrescriptionStatus.PartiallyDispensed, rx.Status);
    }

    [Fact]
    public async Task Billing_PreventsOverpaymentAndEnforcesRefundLimit()
    {
        await using var db = CreateInMemoryDb();
        var facId = Guid.NewGuid();
        var patId = Guid.NewGuid();
        var inv = new Invoice
        {
            InvoiceCode = "HD-01",
            PatientId = patId,
            FacilityId = facId,
            TotalAmount = 200000m,
            PatientResponsibilityAmount = 200000m,
            Status = InvoiceStatus.Issued
        };
        db.Invoices.Add(inv);
        await db.SaveChangesAsync();

        var adapters = new FakeBillingAdapters();
        var billing = new BillingService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService(), adapters, adapters, adapters);

        var overpay = await billing.RecordPaymentAsync(inv.Id, 250000m, PaymentMethod.Cash, isAdvance: false);
        Assert.False(overpay.Succeeded);

        var pay = await billing.RecordPaymentAsync(inv.Id, 150000m, PaymentMethod.Cash, isAdvance: false);
        Assert.True(pay.Succeeded);
        Assert.Equal(InvoiceStatus.PartiallyPaid, inv.Status);

        var overRefund = await billing.ProcessRefundAsync(pay.Data, 180000m, "Hoàn quá mức");
        Assert.False(overRefund.Succeeded);

        var okRefund = await billing.ProcessRefundAsync(pay.Data, 50000m, "Hủy 1 xét nghiệm");
        Assert.True(okRefund.Succeeded);
        Assert.Equal(100000m, inv.PaidAmount);
        Assert.Equal(50000m, inv.RefundedAmount);
    }

    [Fact]
    public async Task MasterDataAndHrCrud_WorksEndToEndAndProtectsOccupiedBed()
    {
        await using var db = CreateInMemoryDb();
        var hosp = new Hospital { Code = "BV", Name = "BV Test" };
        db.Hospitals.Add(hosp);
        await db.SaveChangesAsync();

        var md = new SmartHospital.Application.MasterData.MasterDataService(db, new FakeAuditService());
        var fac = await md.CreateFacilityAsync(hosp.Id, "CS2", "Cơ sở 2", "024", "HN", false);
        Assert.True(fac.Succeeded);

        var dept = await md.CreateDepartmentAsync(fac.Data, "NOI", "Nội", DepartmentType.ClinicalInpatient, "T2");
        var spec = await md.CreateSpecialtyAsync("NOI-TQ", "Nội tổng quát", null);
        var room = await md.CreateRoomAsync(fac.Data, dept.Data, "P201", "Phòng 201", RoomType.InpatientWard, "T2");
        var clinic = await md.CreateClinicAsync(fac.Data, dept.Data, spec.Data, room.Data, "PK2", "PK Nội", "N2");
        var bed = await md.CreateBedAsync(fac.Data, dept.Data, room.Data, "G201", "Giường 1", 500000m);
        var svc = await md.CreateServiceAsync("DV1", "Khám mới", "Khám bệnh", dept.Data, 200000m);
        var lab = await md.CreateLabTestAsync("XN1", "Test XN", "Sinh hóa", "Máu", "mg/L", "0-5", 0, 5, 100000m);
        var img = await md.CreateImagingServiceAsync("CD1", "Siêu âm", "Siêu âm", 250000m);
        var med = await md.CreateMedicineAsync("TH1", "Thuốc A", "HC A", "500mg", "Viên", "Viên", "Uống", 5000m, 10, false);
        var sup = await md.CreateSupplierAsync("NCC1", "NCC Test", null, "090", null, null);
        var invItem = await md.CreateInventoryItemAsync("VT1", "Bơm tiêm", "Vật tư", "Cái", 2000m, 10, 100);
        var icd = await md.CreateDiagnosisAsync("A00", "Bệnh tả", "Truyền nhiễm");
        var pl = await md.CreatePriceListAsync("BG1", "Bảng giá 1", new DateOnly(2026, 1, 1), null, true);

        Assert.All([dept, spec, room, clinic, bed, svc, lab, img, med, sup, invItem, icd, pl], r => Assert.True(r.Succeeded));
        Assert.True((await md.UpsertServicePriceAsync(pl.Data, "Service", svc.Data, 220000m)).Succeeded);

        // Update checks
        Assert.True((await md.UpdateBedAsync(bed.Data, room.Data, "Giường 1 VIP", 700000m, BedStatus.Available, true)).Succeeded);
        Assert.True((await md.UpdateMedicineAsync(med.Data, "Thuốc A+", "HC A", "500mg", "Viên", "Viên", "Uống", 6000m, 10, false, true)).Succeeded);

        // Occupied bed protection
        var ops = new InpatientAndHospitalOpsService(db, new FakeCodeGenerator(), new FakeCurrentUserService(), new FakeAuditService());
        Assert.True((await ops.AdmitPatientAsync(Guid.NewGuid(), Guid.NewGuid(), fac.Data, dept.Data, bed.Data, "Nội trú")).Succeeded);
        var deactivateOccupied = await md.UpdateBedAsync(bed.Data, room.Data, "Giường 1 VIP", 700000m, BedStatus.Occupied, false);
        Assert.False(deactivateOccupied.Succeeded);

        // HR CRUD
        var emp = await ops.CreateEmployeeAsync(fac.Data, dept.Data, "NV01", "BS Test", "Bác sĩ", "090", null, new DateOnly(2026, 1, 1));
        Assert.True(emp.Succeeded);
        Assert.True((await ops.UpdateEmployeeAsync(emp.Data, dept.Data, "BS Test Updated", "Trưởng khoa", "091", null, true, false)).Succeeded);
        var shift = await ops.CreateShiftScheduleAsync(fac.Data, dept.Data, emp.Data, clinic.Data, new DateOnly(2026, 11, 26), "Sáng", new TimeOnly(7, 30), new TimeOnly(11, 30));
        Assert.True(shift.Succeeded);
        Assert.True((await ops.DeleteShiftScheduleAsync(shift.Data)).Succeeded);
    }
}
