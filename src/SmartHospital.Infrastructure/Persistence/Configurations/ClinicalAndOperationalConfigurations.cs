using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Inpatient;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Patients;
using SmartHospital.Domain.Pharmacy;
using SmartHospital.Domain.Reception;

namespace SmartHospital.Infrastructure.Persistence.Configurations;

public class ClinicalAndOperationalConfigurations :
    IEntityTypeConfiguration<Patient>,
    IEntityTypeConfiguration<Appointment>,
    IEntityTypeConfiguration<Encounter>,
    IEntityTypeConfiguration<QueueTicket>,
    IEntityTypeConfiguration<AdmissionRequest>,
    IEntityTypeConfiguration<ClinicalNote>,
    IEntityTypeConfiguration<ClinicalNoteVersion>,
    IEntityTypeConfiguration<Diagnosis>,
    IEntityTypeConfiguration<ClinicalOrder>,
    IEntityTypeConfiguration<Prescription>,
    IEntityTypeConfiguration<LabTest>,
    IEntityTypeConfiguration<LabOrder>,
    IEntityTypeConfiguration<Specimen>,
    IEntityTypeConfiguration<LabResult>,
    IEntityTypeConfiguration<ImagingService>,
    IEntityTypeConfiguration<ImagingOrder>,
    IEntityTypeConfiguration<Medicine>,
    IEntityTypeConfiguration<Warehouse>,
    IEntityTypeConfiguration<MedicineBatch>,
    IEntityTypeConfiguration<StockTransaction>,
    IEntityTypeConfiguration<PrescriptionDispensing>,
    IEntityTypeConfiguration<InventoryItem>,
    IEntityTypeConfiguration<InventoryBatch>,
    IEntityTypeConfiguration<PurchaseRequest>,
    IEntityTypeConfiguration<Admission>,
    IEntityTypeConfiguration<BedAssignment>,
    IEntityTypeConfiguration<EmergencyIntake>,
    IEntityTypeConfiguration<SurgeryRequest>,
    IEntityTypeConfiguration<ServiceCatalog>,
    IEntityTypeConfiguration<ServicePriceList>,
    IEntityTypeConfiguration<Invoice>,
    IEntityTypeConfiguration<Payment>,
    IEntityTypeConfiguration<Refund>,
    IEntityTypeConfiguration<InsuranceClaim>,
    IEntityTypeConfiguration<AuditLog>,
    IEntityTypeConfiguration<IntegrationMessage>,
    IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.Property(x => x.PatientCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(30);
        builder.Property(x => x.NationalId).HasMaxLength(30);
        builder.HasIndex(x => x.PatientCode).IsUnique();
        builder.HasIndex(x => x.NormalizedName);
        builder.HasIndex(x => x.PhoneNumber);
        builder.HasIndex(x => x.NationalId);
    }

    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.Property(x => x.AppointmentCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.AppointmentCode).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.ScheduledAtUtc, x.Status });
        builder.HasIndex(x => x.PatientId);
    }

    public void Configure(EntityTypeBuilder<Encounter> builder)
    {
        builder.ToTable("Encounters");
        builder.Property(x => x.EncounterCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.EncounterCode).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.Status, x.StartedAtUtc });
        builder.HasIndex(x => x.PatientId);
    }

    public void Configure(EntityTypeBuilder<QueueTicket> builder)
    {
        builder.ToTable("QueueTickets");
        builder.Property(x => x.TicketNumber).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.ClinicId, x.QueueDate, x.SequenceNumber }).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.QueueDate, x.Status });
    }

    public void Configure(EntityTypeBuilder<AdmissionRequest> builder)
    {
        builder.ToTable("AdmissionRequests");
        builder.Property(x => x.RequestCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RequestCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ClinicalNote> builder)
    {
        builder.ToTable("ClinicalNotes");
        builder.Property(x => x.ChiefComplaint).HasMaxLength(2000);
        builder.Property(x => x.HistoryOfPresentIllness).HasMaxLength(4000);
        builder.Property(x => x.PastMedicalHistory).HasMaxLength(4000);
        builder.Property(x => x.PhysicalExam).HasMaxLength(4000);
        builder.Property(x => x.PreliminaryDiagnosis).HasMaxLength(1000);
        builder.Property(x => x.FinalDiagnosis).HasMaxLength(1000);
        builder.Property(x => x.TreatmentPlan).HasMaxLength(4000);
        builder.Property(x => x.PatientInstructions).HasMaxLength(2000);
        builder.HasIndex(x => x.EncounterId);
    }

    public void Configure(EntityTypeBuilder<ClinicalNoteVersion> builder)
    {
        builder.ToTable("ClinicalNoteVersions");
        builder.Property(x => x.SnapshotJson).HasMaxLength(4000000);
        builder.HasIndex(x => new { x.ClinicalNoteId, x.VersionNumber }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("Diagnoses");
        builder.Property(x => x.IcdCode).HasMaxLength(30).IsRequired();
        builder.HasIndex(x => x.IcdCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ClinicalOrder> builder)
    {
        builder.ToTable("ClinicalOrders");
        builder.Property(x => x.OrderCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.OrderCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.Property(x => x.PrescriptionCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.PrescriptionCode).IsUnique();
        builder.HasIndex(x => x.EncounterId);
        builder.HasIndex(x => x.PatientId);
    }

    public void Configure(EntityTypeBuilder<LabTest> builder)
    {
        builder.ToTable("LabTests");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<LabOrder> builder)
    {
        builder.ToTable("LabOrders");
        builder.Property(x => x.OrderCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.OrderCode).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.Status });
    }

    public void Configure(EntityTypeBuilder<Specimen> builder)
    {
        builder.ToTable("Specimens");
        builder.Property(x => x.Barcode).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => x.Barcode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<LabResult> builder)
    {
        builder.ToTable("LabResults");
        builder.HasIndex(x => x.LabOrderItemId).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ImagingService> builder)
    {
        builder.ToTable("ImagingServices");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ImagingOrder> builder)
    {
        builder.ToTable("ImagingOrders");
        builder.Property(x => x.OrderCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.OrderCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Medicine> builder)
    {
        builder.ToTable("Medicines");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => new { x.FacilityId, x.Code }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<MedicineBatch> builder)
    {
        builder.ToTable("MedicineBatches");
        builder.Property(x => x.BatchNumber).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.MedicineId, x.BatchNumber }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<StockTransaction> builder)
    {
        builder.ToTable("StockTransactions");
        builder.Property(x => x.TransactionCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.TransactionCode).IsUnique();
        builder.HasIndex(x => new { x.WarehouseId, x.TransactionDateUtc });
    }

    public void Configure(EntityTypeBuilder<PrescriptionDispensing> builder)
    {
        builder.ToTable("PrescriptionDispensings");
        builder.Property(x => x.DispensingCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.DispensingCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<InventoryBatch> builder)
    {
        builder.ToTable("InventoryBatches");
        builder.Property(x => x.BatchNumber).HasMaxLength(60).IsRequired();
        builder.HasIndex(x => new { x.WarehouseId, x.InventoryItemId, x.BatchNumber }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("PurchaseRequests");
        builder.Property(x => x.RequestCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RequestCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Admission> builder)
    {
        builder.ToTable("Admissions");
        builder.Property(x => x.AdmissionCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.AdmissionCode).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.Status });
    }

    public void Configure(EntityTypeBuilder<BedAssignment> builder)
    {
        builder.ToTable("BedAssignments");
        builder.HasIndex(x => x.BedId)
            .IsUnique()
            .HasFilter("[IsCurrent] = 1");
    }

    public void Configure(EntityTypeBuilder<EmergencyIntake> builder)
    {
        builder.ToTable("EmergencyIntakes");
        builder.Property(x => x.EmergencyCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.EmergencyCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<SurgeryRequest> builder)
    {
        builder.ToTable("SurgeryRequests");
        builder.Property(x => x.RequestCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RequestCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ServiceCatalog> builder)
    {
        builder.ToTable("ServiceCatalogs");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ServicePriceList> builder)
    {
        builder.ToTable("ServicePriceLists");
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }

    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.Property(x => x.InvoiceCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.InvoiceCode).IsUnique();
        builder.HasIndex(x => new { x.FacilityId, x.Status, x.IssuedAtUtc });
        builder.HasIndex(x => x.PatientId);
    }

    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(x => x.ReceiptNumber).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ReceiptNumber).IsUnique();
        builder.HasIndex(x => x.PatientId);
    }

    public void Configure(EntityTypeBuilder<Refund> builder)
    {
        builder.ToTable("Refunds");
        builder.Property(x => x.RefundCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.RefundCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<InsuranceClaim> builder)
    {
        builder.ToTable("InsuranceClaims");
        builder.Property(x => x.ClaimCode).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.ClaimCode).IsUnique();
    }

    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BeforeSummaryJson).HasMaxLength(4000000);
        builder.Property(x => x.AfterSummaryJson).HasMaxLength(4000000);
        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.EntityType, x.EntityId });
        builder.HasIndex(x => x.CorrelationId);
    }

    public void Configure(EntityTypeBuilder<IntegrationMessage> builder)
    {
        builder.ToTable("IntegrationMessages");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.Property(x => x.RequestSummary).HasMaxLength(4000);
        builder.Property(x => x.ResponseSummary).HasMaxLength(4000);
        builder.HasIndex(x => new { x.IntegrationType, x.IdempotencyKey });
        builder.HasIndex(x => x.CreatedAtUtc);
    }

    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");
        builder.Property(x => x.Key).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(4000).IsRequired();
        builder.HasIndex(x => x.Key).IsUnique();
    }
}
