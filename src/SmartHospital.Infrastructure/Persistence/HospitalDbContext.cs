using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Identity;
using SmartHospital.Domain.Inpatient;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Patients;
using SmartHospital.Domain.Pharmacy;
using SmartHospital.Domain.Reception;
using SmartHospital.Infrastructure.Identity;

namespace SmartHospital.Infrastructure.Persistence;

public class HospitalDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>, IHospitalDbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public HospitalDbContext(DbContextOptions<HospitalDbContext> options) : base(options)
    {
    }

    public HospitalDbContext(
        DbContextOptions<HospitalDbContext> options,
        ICurrentUserService currentUserService) : base(options)
    {
        _currentUserService = currentUserService;
    }

    // Organization & Security
    public DbSet<Hospital> Hospitals => Set<Hospital>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Bed> Beds => Set<Bed>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<DoctorProfile> DoctorProfiles => Set<DoctorProfile>();
    public DbSet<NurseProfile> NurseProfiles => Set<NurseProfile>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<LeaveRecord> LeaveRecords => Set<LeaveRecord>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserFacilityScope> UserFacilityScopes => Set<UserFacilityScope>();
    public DbSet<UserDepartmentScope> UserDepartmentScopes => Set<UserDepartmentScope>();

    // Patient
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<PatientIdentifier> PatientIdentifiers => Set<PatientIdentifier>();
    public DbSet<PatientContact> PatientContacts => Set<PatientContact>();
    public DbSet<PatientEmergencyContact> PatientEmergencyContacts => Set<PatientEmergencyContact>();
    public DbSet<PatientAllergy> PatientAllergies => Set<PatientAllergy>();
    public DbSet<PatientAlert> PatientAlerts => Set<PatientAlert>();
    public DbSet<PatientMergeHistory> PatientMergeHistories => Set<PatientMergeHistory>();

    // Reception & Appointment
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<QueueTicket> QueueTickets => Set<QueueTicket>();
    public DbSet<EncounterStatusHistory> EncounterStatusHistories => Set<EncounterStatusHistory>();
    public DbSet<AdmissionRequest> AdmissionRequests => Set<AdmissionRequest>();

    // Clinical
    public DbSet<ClinicalNote> ClinicalNotes => Set<ClinicalNote>();
    public DbSet<ClinicalNoteVersion> ClinicalNoteVersions => Set<ClinicalNoteVersion>();
    public DbSet<VitalSign> VitalSigns => Set<VitalSign>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<EncounterDiagnosis> EncounterDiagnoses => Set<EncounterDiagnosis>();
    public DbSet<ClinicalOrder> ClinicalOrders => Set<ClinicalOrder>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<ClinicalAttachment> ClinicalAttachments => Set<ClinicalAttachment>();

    // Laboratory & Imaging
    public DbSet<LabTest> LabTests => Set<LabTest>();
    public DbSet<LabOrder> LabOrders => Set<LabOrder>();
    public DbSet<LabOrderItem> LabOrderItems => Set<LabOrderItem>();
    public DbSet<Specimen> Specimens => Set<Specimen>();
    public DbSet<LabResult> LabResults => Set<LabResult>();
    public DbSet<LabResultVersion> LabResultVersions => Set<LabResultVersion>();
    public DbSet<ImagingService> ImagingServices => Set<ImagingService>();
    public DbSet<ImagingOrder> ImagingOrders => Set<ImagingOrder>();
    public DbSet<ImagingReport> ImagingReports => Set<ImagingReport>();

    // Pharmacy & Inventory
    public DbSet<Medicine> Medicines => Set<Medicine>();
    public DbSet<MedicineUnit> MedicineUnits => Set<MedicineUnit>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<MedicineBatch> MedicineBatches => Set<MedicineBatch>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockTransactionItem> StockTransactionItems => Set<StockTransactionItem>();
    public DbSet<PrescriptionDispensing> PrescriptionDispensings => Set<PrescriptionDispensing>();
    public DbSet<PrescriptionDispensingItem> PrescriptionDispensingItems => Set<PrescriptionDispensingItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<PurchaseRequest> PurchaseRequests => Set<PurchaseRequest>();

    // Inpatient, Nursing, Emergency, Surgery
    public DbSet<Admission> Admissions => Set<Admission>();
    public DbSet<BedAssignment> BedAssignments => Set<BedAssignment>();
    public DbSet<BedStatusHistory> BedStatusHistories => Set<BedStatusHistory>();
    public DbSet<NursingAssignment> NursingAssignments => Set<NursingAssignment>();
    public DbSet<NursingNote> NursingNotes => Set<NursingNote>();
    public DbSet<NursingObservation> NursingObservations => Set<NursingObservation>();
    public DbSet<MedicationAdministration> MedicationAdministrations => Set<MedicationAdministration>();
    public DbSet<ShiftHandoverNote> ShiftHandoverNotes => Set<ShiftHandoverNote>();
    public DbSet<EmergencyIntake> EmergencyIntakes => Set<EmergencyIntake>();
    public DbSet<SurgeryRequest> SurgeryRequests => Set<SurgeryRequest>();

    // Billing
    public DbSet<ServiceCatalog> ServiceCatalogs => Set<ServiceCatalog>();
    public DbSet<ServicePriceList> ServicePriceLists => Set<ServicePriceList>();
    public DbSet<ServicePrice> ServicePrices => Set<ServicePrice>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentAllocation> PaymentAllocations => Set<PaymentAllocation>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<BillingAdjustment> BillingAdjustments => Set<BillingAdjustment>();
    public DbSet<InsuranceProfile> InsuranceProfiles => Set<InsuranceProfile>();
    public DbSet<InsuranceClaim> InsuranceClaims => Set<InsuranceClaim>();

    // Audit & Integration
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<IntegrationMessage> IntegrationMessages => Set<IntegrationMessage>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<decimal>().HavePrecision(18, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(500);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Clean Identity table names
        builder.Entity<ApplicationUser>().ToTable("Users");
        builder.Entity<ApplicationRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserTokens");

        builder.ApplyConfigurationsFromAssembly(typeof(HospitalDbContext).Assembly);

        // Configure RowVersion and default Restrict delete behavior for all entities
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            if (typeof(AuditableEntity).IsAssignableFrom(entityType.ClrType))
            {
                builder.Entity(entityType.ClrType)
                    .Property(nameof(AuditableEntity.RowVersion))
                    .IsRowVersion();
            }

            foreach (var fk in entityType.GetForeignKeys())
            {
                if (fk.DeleteBehavior == DeleteBehavior.Cascade &&
                    !entityType.ClrType.Namespace?.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal) == true)
                {
                    fk.DeleteBehavior = DeleteBehavior.Restrict;
                }
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var actor = _currentUserService?.UserName;
        if (string.IsNullOrWhiteSpace(actor))
        {
            actor = "system";
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity.CreatedAtUtc == default)
                {
                    entry.Entity.CreatedAtUtc = now;
                }
                entry.Entity.CreatedBy ??= actor;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAtUtc = now;
                entry.Entity.UpdatedBy = actor;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
