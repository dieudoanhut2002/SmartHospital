# Kiến trúc & Sơ đồ cơ sở dữ liệu (ARCHITECTURE & ERD)

## 1. Kiến trúc Modular Monolith

```text
SmartHospital.sln
├── src/SmartHospital.Domain          (POCO Entities, Enums, Constants, DomainException)
├── src/SmartHospital.Contracts       (DTOs, PagedRequest/Result, ApiErrorResponse)
├── src/SmartHospital.Application     (Use-case Services, IHospitalDbContext, Integration Interfaces)
├── src/SmartHospital.Infrastructure  (HospitalDbContext, EF Migrations, DatabaseInitializer,
│                                      HospitalDataSeeder, Identity, Audit, LocalDocumentStorage,
│                                      Demo/Sandbox Adapters)
└── src/SmartHospital.Web             (ASP.NET Core MVC Areas, API v1, Middleware,
                                       Permission Policy Authorization, Print Views)
```

### Quy tắc phụ thuộc
- `Domain` không tham chiếu bất kỳ layer nào.
- `Application` chỉ phụ thuộc `Domain`, `Contracts` và `Microsoft.EntityFrameworkCore`.
- `Infrastructure` triển khai `IHospitalDbContext`, ASP.NET Core Identity, SQL Server bootstrap và adapter tích hợp.
- `Web` xử lý HTTP/UI, CSRF, rate limiting, cookie security, permission policies và gọi `Application`.

## 2. Sơ đồ quan hệ dữ liệu cốt lõi (ERD)

```mermaid
erDiagram
    Hospital ||--o{ Facility : has
    Facility ||--o{ Department : has
    Facility ||--o{ Room : has
    Facility ||--o{ Clinic : has
    Department ||--o{ Room : contains
    Room ||--o{ Bed : contains
    Facility ||--o{ Warehouse : owns

    Patient ||--o{ PatientAllergy : has
    Patient ||--o{ PatientAlert : has
    Patient ||--o{ Appointment : books
    Patient ||--o{ Encounter : visits
    Appointment ||--o| Encounter : converts_to
    Encounter ||--o{ QueueTicket : receives
    Encounter ||--o| ClinicalNote : documents
    ClinicalNote ||--o{ ClinicalNoteVersion : versions
    Encounter ||--o{ VitalSign : records
    Encounter ||--o{ LabOrder : orders
    LabOrder ||--o{ LabOrderItem : contains
    LabOrder ||--o{ Specimen : collects
    LabOrderItem ||--o| LabResult : yields
    LabResult ||--o{ LabResultVersion : versions
    Encounter ||--o{ ImagingOrder : orders
    ImagingOrder ||--o| ImagingReport : yields
    Encounter ||--o{ Prescription : prescribes
    Prescription ||--o{ PrescriptionItem : contains

    Prescription ||--o{ PrescriptionDispensing : fulfilled_by
    Warehouse ||--o{ MedicineBatch : stocks
    Warehouse ||--o{ StockTransaction : ledger
    StockTransaction ||--o{ StockTransactionItem : details

    Encounter ||--o{ AdmissionRequest : requests
    Admission ||--o{ BedAssignment : occupies
    Bed ||--o{ BedAssignment : assigned_to
    Admission ||--o{ NursingNote : cares
    Admission ||--o{ NursingObservation : monitors
    Admission ||--o{ MedicationAdministration : administers

    Patient ||--o{ Invoice : billed
    Invoice ||--o{ InvoiceItem : lines
    Payment ||--o{ PaymentAllocation : settles
    Invoice ||--o{ PaymentAllocation : receives
    Payment ||--o{ Refund : refunds
```

## 3. Các ràng buộc toàn vẹn chính
- **Khóa chính & Concurrency:** `Guid` cho entity nghiệp vụ (`rowversion` optimistic concurrency), `bigint` cho `AuditLogs` và `IntegrationMessages`.
- **Không Cascade Delete** trên dữ liệu y tế/tài chính (`DeleteBehavior.Restrict`).
- **Ràng buộc duy nhất (Unique Indexes):** `Patient.PatientCode`, `Encounter.EncounterCode`, `QueueTicket(FacilityId, ClinicId, QueueDate, SequenceNumber)`, `Specimen.Barcode`, `BedAssignment(BedId) WHERE IsCurrent = 1`, `Invoice.InvoiceCode`, `Payment.ReceiptNumber`, `StockTransaction.TransactionCode`.
- **Snapshot dữ liệu:** Đơn giá và tên dịch vụ/thuốc được chụp lại tại thời điểm tạo chỉ định/hóa đơn.
