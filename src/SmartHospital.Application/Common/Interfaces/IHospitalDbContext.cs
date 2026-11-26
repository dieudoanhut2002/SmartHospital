using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Identity;
using SmartHospital.Domain.Inpatient;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Patients;
using SmartHospital.Domain.Pharmacy;
using SmartHospital.Domain.Reception;

namespace SmartHospital.Application.Common.Interfaces;

public interface IHospitalDbContext
{
    DatabaseFacade Database { get; }

    // Organization & Security
    DbSet<Hospital> Hospitals { get; }
    DbSet<Facility> Facilities { get; }
    DbSet<Department> Departments { get; }
    DbSet<Specialty> Specialties { get; }
    DbSet<Room> Rooms { get; }
    DbSet<Clinic> Clinics { get; }
    DbSet<Bed> Beds { get; }
    DbSet<Employee> Employees { get; }
    DbSet<DoctorProfile> DoctorProfiles { get; }
    DbSet<NurseProfile> NurseProfiles { get; }
    DbSet<ShiftSchedule> ShiftSchedules { get; }
    DbSet<LeaveRecord> LeaveRecords { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserFacilityScope> UserFacilityScopes { get; }
    DbSet<UserDepartmentScope> UserDepartmentScopes { get; }

    // Patient
    DbSet<Patient> Patients { get; }
    DbSet<PatientIdentifier> PatientIdentifiers { get; }
    DbSet<PatientContact> PatientContacts { get; }
    DbSet<PatientEmergencyContact> PatientEmergencyContacts { get; }
    DbSet<PatientAllergy> PatientAllergies { get; }
    DbSet<PatientAlert> PatientAlerts { get; }
    DbSet<PatientMergeHistory> PatientMergeHistories { get; }

    // Reception & Appointment
    DbSet<Appointment> Appointments { get; }
    DbSet<Encounter> Encounters { get; }
    DbSet<QueueTicket> QueueTickets { get; }
    DbSet<EncounterStatusHistory> EncounterStatusHistories { get; }
    DbSet<AdmissionRequest> AdmissionRequests { get; }

    // Clinical
    DbSet<ClinicalNote> ClinicalNotes { get; }
    DbSet<ClinicalNoteVersion> ClinicalNoteVersions { get; }
    DbSet<VitalSign> VitalSigns { get; }
    DbSet<Diagnosis> Diagnoses { get; }
    DbSet<EncounterDiagnosis> EncounterDiagnoses { get; }
    DbSet<ClinicalOrder> ClinicalOrders { get; }
    DbSet<ServiceOrder> ServiceOrders { get; }
    DbSet<Prescription> Prescriptions { get; }
    DbSet<PrescriptionItem> PrescriptionItems { get; }
    DbSet<ClinicalAttachment> ClinicalAttachments { get; }

    // Laboratory & Imaging
    DbSet<LabTest> LabTests { get; }
    DbSet<LabOrder> LabOrders { get; }
    DbSet<LabOrderItem> LabOrderItems { get; }
    DbSet<Specimen> Specimens { get; }
    DbSet<LabResult> LabResults { get; }
    DbSet<LabResultVersion> LabResultVersions { get; }
    DbSet<ImagingService> ImagingServices { get; }
    DbSet<ImagingOrder> ImagingOrders { get; }
    DbSet<ImagingReport> ImagingReports { get; }

    // Pharmacy & Inventory
    DbSet<Medicine> Medicines { get; }
    DbSet<MedicineUnit> MedicineUnits { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<MedicineBatch> MedicineBatches { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<StockTransactionItem> StockTransactionItems { get; }
    DbSet<PrescriptionDispensing> PrescriptionDispensings { get; }
    DbSet<PrescriptionDispensingItem> PrescriptionDispensingItems { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<InventoryItem> InventoryItems { get; }
    DbSet<InventoryBatch> InventoryBatches { get; }
    DbSet<PurchaseRequest> PurchaseRequests { get; }

    // Inpatient, Nursing, Emergency, Surgery
    DbSet<Admission> Admissions { get; }
    DbSet<BedAssignment> BedAssignments { get; }
    DbSet<BedStatusHistory> BedStatusHistories { get; }
    DbSet<NursingAssignment> NursingAssignments { get; }
    DbSet<NursingNote> NursingNotes { get; }
    DbSet<NursingObservation> NursingObservations { get; }
    DbSet<MedicationAdministration> MedicationAdministrations { get; }
    DbSet<ShiftHandoverNote> ShiftHandoverNotes { get; }
    DbSet<EmergencyIntake> EmergencyIntakes { get; }
    DbSet<SurgeryRequest> SurgeryRequests { get; }

    // Billing
    DbSet<ServiceCatalog> ServiceCatalogs { get; }
    DbSet<ServicePriceList> ServicePriceLists { get; }
    DbSet<ServicePrice> ServicePrices { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceItem> InvoiceItems { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentAllocation> PaymentAllocations { get; }
    DbSet<Refund> Refunds { get; }
    DbSet<BillingAdjustment> BillingAdjustments { get; }
    DbSet<InsuranceProfile> InsuranceProfiles { get; }
    DbSet<InsuranceClaim> InsuranceClaims { get; }

    // Audit & Integration
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Document> Documents { get; }
    DbSet<Notification> Notifications { get; }
    DbSet<IntegrationMessage> IntegrationMessages { get; }
    DbSet<SystemSetting> SystemSettings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
