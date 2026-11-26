using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SmartHospital.Application.Billing;
using SmartHospital.Application.Clinical;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.Laboratory;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Pharmacy;
using SmartHospital.Application.Reception;
using SmartHospital.Domain.Common;
using SmartHospital.Infrastructure.Identity;
using SmartHospital.Infrastructure.Persistence;

namespace SmartHospital.IntegrationTests;

public class SqlServerLocalDbFixture : IAsyncLifetime
{
    public string TestDatabaseName { get; } = $"SmartHospital_Test_{Guid.NewGuid():N}";
    public string ConnectionString => $"Server=(localdb)\\MSSQLLocalDB;Database={TestDatabaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
    public string BootstrapAdminPassword => "AdminTest@12345!";

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        try
        {
            await using var conn = new SqlConnection("Server=(localdb)\\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
            await conn.OpenAsync();
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = $"""
                IF DB_ID('{TestDatabaseName}') IS NOT NULL
                BEGIN
                    ALTER DATABASE [{TestDatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                    DROP DATABASE [{TestDatabaseName}];
                END
                """;
            await cmd.ExecuteNonQueryAsync();
        }
        catch
        {
            // Ignore cleanup failure if DB wasn't created
        }
    }
}

public class SqlServerEndToEndTests : IClassFixture<SqlServerLocalDbFixture>
{
    private readonly SqlServerLocalDbFixture _fixture;

    public SqlServerEndToEndTests(SqlServerLocalDbFixture fixture)
    {
        _fixture = fixture;
    }

    private WebApplicationFactory<Program> CreateFactory(bool autoMigrate = true, bool seedData = true, string? overrideCs = null)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = overrideCs ?? _fixture.ConnectionString,
                    ["Database:AutoMigrate"] = autoMigrate.ToString(),
                    ["Database:SeedInitialData"] = seedData.ToString(),
                    ["Database:SeedDemoData"] = "true",
                    ["BootstrapAdmin:Password"] = _fixture.BootstrapAdminPassword,
                    ["BootstrapAdmin:DemoUserPassword"] = "Demo@123456!"
                });
            });
        });
    }

    [Fact]
    public async Task Bootstrap_CreatesDatabase_SeedsIdempotently_PreservesAdminPassword_AndRunsFullE2EFlows()
    {
        // 1. First boot creates brand new database on SQL Server LocalDB & applies migrations + seed
        using var factory = CreateFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var health = await client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);

        // 2. Verify unauthenticated redirect to login
        var unauth = await client.GetAsync("/Patients/Patients");
        Assert.Equal(HttpStatusCode.Redirect, unauth.StatusCode);
        Assert.Contains("/Account/Login", unauth.Headers.Location?.ToString());

        // 3. Re-running DatabaseInitializer twice does NOT create duplicate seed records and preserves admin password
        using (var scope = factory.Services.CreateScope())
        {
            var cfg = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

            var admin = await userManager.FindByNameAsync("admin");
            Assert.NotNull(admin);
            Assert.True(admin!.MustChangePassword);

            // Change admin password
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            await userManager.ResetPasswordAsync(admin, token, "NewAdmin@98765!");
            admin.MustChangePassword = false;
            await userManager.UpdateAsync(admin);

            var roleCountBefore = await db.Roles.CountAsync();
            var medCountBefore = await db.Medicines.CountAsync();

            // Re-run bootstrap
            await DatabaseInitializer.InitializeDatabaseAsync(factory.Services, cfg);

            var roleCountAfter = await db.Roles.CountAsync();
            var medCountAfter = await db.Medicines.CountAsync();
            Assert.Equal(roleCountBefore, roleCountAfter);
            Assert.Equal(medCountBefore, medCountAfter);

            // Admin password must NOT be reset!
            var refreshedAdmin = await userManager.FindByNameAsync("admin");
            Assert.True(await userManager.CheckPasswordAsync(refreshedAdmin!, "NewAdmin@98765!"));
        }

        // 4. Full End-to-End SQL Server Transaction Flow:
        // Patient -> Appointment -> Encounter/Queue -> Clinical Note (Sign/Amend) -> LabOrder -> Result Verify -> Rx -> Dispense -> Invoice -> Payment -> Refund -> AuditLog
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();
            var patientSvc = scope.ServiceProvider.GetRequiredService<IPatientService>();
            var receptionSvc = scope.ServiceProvider.GetRequiredService<IReceptionService>();
            var clinicalSvc = scope.ServiceProvider.GetRequiredService<IClinicalService>();
            var labSvc = scope.ServiceProvider.GetRequiredService<ILabAndImagingService>();
            var pharmSvc = scope.ServiceProvider.GetRequiredService<IPharmacyAndInventoryService>();
            var billingSvc = scope.ServiceProvider.GetRequiredService<IBillingService>();
            var opsSvc = scope.ServiceProvider.GetRequiredService<IInpatientAndHospitalOpsService>();

            var fac = await db.Facilities.FirstAsync();
            var opd = await db.Departments.FirstAsync(d => d.Code == "OPD");
            var intMed = await db.Departments.FirstAsync(d => d.Code == "INTMED");
            var clinic = await db.Clinics.FirstAsync();
            var labTest = await db.LabTests.FirstAsync();
            var med = await db.Medicines.FirstAsync();
            var wh = await db.Warehouses.FirstAsync(w => w.Code == "KHO-DUOC");

            // Create patient
            var patRes = await patientSvc.CreatePatientAsync(new CreateOrUpdatePatientRequest(
                "Phạm Thị Kiểm Thử", new DateOnly(1992, 3, 15), Gender.Female, "0988777666", null, "001192999888", "Hà Nội", "O+", false, null, null, null));
            Assert.True(patRes.Succeeded);

            // Appointment + CheckIn
            var apptRes = await receptionSvc.CreateAppointmentAsync(new CreateAppointmentRequest(
                patRes.Data, fac.Id, opd.Id, null, clinic.Id, null, DateTimeOffset.UtcNow.AddMinutes(30), "Đau họng, sốt", null));
            Assert.True(apptRes.Succeeded);

            var encRes = await receptionSvc.CreateEncounterWithQueueAsync(new WalkInOrCheckInRequest(
                patRes.Data, fac.Id, opd.Id, clinic.Id, null, null, apptRes.Data, EncounterType.Outpatient, "Sốt 38.5"));
            Assert.True(encRes.Succeeded);

            // Clinical Note Draft -> Sign -> Amend
            await clinicalSvc.RecordVitalSignAsync(new RecordVitalSignRequest(encRes.Data, 120, 80, 82, 38.2m, 18, 98, 160, 52, null));
            await clinicalSvc.SaveClinicalNoteDraftAsync(new SaveClinicalNoteRequest(
                encRes.Data, "Sốt", null, null, "Họng đỏ", "Viêm họng cấp", "Viêm họng cấp", "Uống thuốc theo đơn", "Uống nhiều nước", null));
            Assert.True((await clinicalSvc.SignClinicalNoteAsync(encRes.Data)).Succeeded);
            Assert.True((await clinicalSvc.AmendClinicalNoteAsync(new SaveClinicalNoteRequest(
                encRes.Data, "Sốt", null, null, "Họng đỏ, amidan sưng", "Viêm họng", "Viêm họng cấp", "Uống thuốc + tái khám", null, null), "Bổ sung khám amidan")).Succeeded);

            // Lab Order -> Specimen -> Enter Result -> Verify
            var labOrderRes = await clinicalSvc.CreateLabOrderAsync(encRes.Data, [labTest.Id], "Kiểm tra công thức máu");
            Assert.True(labOrderRes.Succeeded);
            Assert.True((await labSvc.CollectSpecimenAsync(labOrderRes.Data, "Tủ A")).Succeeded);
            Assert.True((await labSvc.ReceiveSpecimenAsync(labOrderRes.Data)).Succeeded);
            var labOrder = await labSvc.GetLabOrderDetailAsync(labOrderRes.Data);
            var labItem = labOrder!.Items.First();
            Assert.True((await labSvc.EnterLabResultAsync(labItem.Id, "8.2", 8.2m, null)).Succeeded);
            var labResult = await db.LabResults.FirstAsync(r => r.LabOrderItemId == labItem.Id);
            Assert.True((await labSvc.VerifyLabResultAsync(labResult.Id)).Succeeded);

            // Prescription -> Dispense
            var rxRes = await clinicalSvc.CreatePrescriptionAsync(encRes.Data,
                [new CreatePrescriptionItemInput(med.Id, "Sáng 1 viên, Tối 1 viên", "Uống", 5, 10m)], "Viêm họng", null);
            Assert.True(rxRes.Succeeded);
            var rx = await pharmSvc.GetPrescriptionDetailAsync(rxRes.Data);
            var dispRes = await pharmSvc.DispensePrescriptionAsync(rxRes.Data, wh.Id,
                [new DispenseItemInput(rx!.Items.First().Id, 10m)]);
            Assert.True(dispRes.Succeeded);

            // Complete encounter
            Assert.True((await clinicalSvc.CompleteEncounterAsync(encRes.Data)).Succeeded);

            // Billing: Create Invoice -> Collect Payment -> Refund partial
            var invRes = await billingSvc.CreateInvoiceFromUnbilledChargesAsync(patRes.Data, fac.Id, encRes.Data);
            Assert.True(invRes.Succeeded);
            var invoice = await billingSvc.GetInvoiceDetailAsync(invRes.Data);
            Assert.NotNull(invoice);
            Assert.True(invoice!.TotalAmount > 0);

            var payRes = await billingSvc.RecordPaymentAsync(invoice.Id, invoice.PatientResponsibilityAmount, PaymentMethod.Cash);
            Assert.True(payRes.Succeeded);

            var refundRes = await billingSvc.ProcessRefundAsync(payRes.Data, 10000m, "Hoàn chênh lệch kiểm thử");
            Assert.True(refundRes.Succeeded);

            // Inpatient flow: Admit -> Transfer -> Discharge -> Bed Cleaning -> Available
            var beds = await db.Beds.Where(b => b.Status == BedStatus.Available).Take(2).ToListAsync();
            var admRes = await opsSvc.AdmitPatientAsync(patRes.Data, encRes.Data, fac.Id, intMed.Id, beds[0].Id, "Theo dõi sốt");
            Assert.True(admRes.Succeeded);
            Assert.True((await opsSvc.TransferBedAsync(admRes.Data, beds[1].Id, "Chuyển phòng")).Succeeded);
            Assert.True((await opsSvc.DischargePatientAsync(admRes.Data, "Hết sốt, ổn định", "Nghỉ ngơi", null)).Succeeded);
            Assert.True((await opsSvc.UpdateBedStatusAsync(beds[1].Id, BedStatus.Available, "Đã vệ sinh")).Succeeded);

            // Audit logs recorded
            Assert.True(await db.AuditLogs.CountAsync() >= 5);
        }

        // 5. Web RBAC Smoke Test: demo receptionist login succeeds & cannot access Admin/Users
        var receptionClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });
        var loginPageHtml = await receptionClient.GetStringAsync("/Account/Login");
        var csrfToken = ExtractAntiforgeryToken(loginPageHtml);

        var loginResp = await receptionClient.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["UserName"] = "reception.demo",
            ["Password"] = "Demo@123456!",
            ["__RequestVerificationToken"] = csrfToken
        }));
        loginResp.EnsureSuccessStatusCode();

        // Receptionist can access Patients
        var patientsResp = await receptionClient.GetAsync("/Patients/Patients");
        Assert.Equal(HttpStatusCode.OK, patientsResp.StatusCode);

        // Receptionist is blocked from Admin/Users (403 Forbidden -> AccessDenied)
        var noRedirectClient = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        // Copy auth cookie
        if (loginResp.RequestMessage?.Headers.TryGetValues("Cookie", out _) == true) { }
        var adminResp = await receptionClient.GetAsync("/Admin/Users");
        var adminBody = await adminResp.Content.ReadAsStringAsync();
        Assert.True(adminResp.StatusCode == HttpStatusCode.Forbidden || adminBody.Contains("403 - Không có quyền truy cập"));
    }

    [Fact]
    public async Task Bootstrap_WhenAutoMigrateFalseAndDbMissing_ThrowsClearError()
    {
        var nonExistentDb = $"SmartHospital_Missing_{Guid.NewGuid():N}";
        var cs = $"Server=(localdb)\\MSSQLLocalDB;Database={nonExistentDb};Trusted_Connection=True;TrustServerCertificate=True";

        var services = new ServiceCollection();
        services.AddLogging();
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = cs,
            ["Database:AutoMigrate"] = "false",
            ["Database:SeedInitialData"] = "false"
        }).Build();
        services.AddSingleton<IConfiguration>(cfg);
        services.AddDbContext<HospitalDbContext>(o => o.UseSqlServer(cs));
        var sp = services.BuildServiceProvider();

        await Assert.ThrowsAsync<DatabaseBootstrapException>(() =>
            DatabaseInitializer.InitializeDatabaseAsync(sp, cfg));
    }

    private static string ExtractAntiforgeryToken(string html)
    {
        const string marker = "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"";
        var idx = html.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0)
        {
            const string altMarker = "__RequestVerificationToken\" value=\"";
            idx = html.IndexOf(altMarker, StringComparison.Ordinal);
            if (idx < 0) return string.Empty;
            idx += altMarker.Length;
        }
        else
        {
            idx += marker.Length;
        }

        var end = html.IndexOf('"', idx);
        return end > idx ? html[idx..end] : string.Empty;
    }
}
