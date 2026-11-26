namespace SmartHospital.Domain.Common;

public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2,
    Other = 3
}

public enum ScopeType
{
    Hospital = 1,
    Facility = 2,
    Department = 3,
    Clinic = 4,
    Warehouse = 5
}

public enum DepartmentType
{
    Administration = 1,
    Reception = 2,
    Outpatient = 3,
    ClinicalInpatient = 4,
    Emergency = 5,
    Nursing = 6,
    Laboratory = 7,
    Radiology = 8,
    Pharmacy = 9,
    Surgery = 10,
    Finance = 11,
    Inventory = 12,
    HumanResources = 13,
    QualityAndPlanning = 14
}

public enum RoomType
{
    Examination = 1,
    InpatientWard = 2,
    EmergencyBay = 3,
    OperatingRoom = 4,
    LaboratoryRoom = 5,
    ImagingRoom = 6,
    PharmacyCounter = 7,
    Administration = 8
}

public enum BedStatus
{
    Available = 1,
    Occupied = 2,
    Cleaning = 3,
    Maintenance = 4,
    Reserved = 5
}

public enum AllergySeverity
{
    Mild = 1,
    Moderate = 2,
    Severe = 3,
    LifeThreatening = 4
}

public enum AppointmentStatus
{
    Booked = 1,
    CheckedIn = 2,
    Waiting = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoShow = 7
}

public enum EncounterType
{
    Outpatient = 1,
    Emergency = 2,
    Inpatient = 3
}

public enum EncounterStatus
{
    Booked = 1,
    CheckedIn = 2,
    Waiting = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6,
    NoShow = 7
}

public enum QueueTicketStatus
{
    Waiting = 1,
    Called = 2,
    InProgress = 3,
    Skipped = 4,
    Transferred = 5,
    Completed = 6,
    Cancelled = 7
}

public enum ClinicalNoteStatus
{
    Draft = 1,
    Signed = 2,
    Amended = 3
}

public enum OrderStatus
{
    Ordered = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4
}

public enum LabOrderStatus
{
    Ordered = 1,
    Collected = 2,
    Received = 3,
    InProgress = 4,
    PendingReview = 5,
    Verified = 6,
    Rejected = 7,
    Cancelled = 8
}

public enum SpecimenStatus
{
    PendingCollection = 1,
    Collected = 2,
    Received = 3,
    Rejected = 4,
    Processed = 5
}

public enum ImagingOrderStatus
{
    Ordered = 1,
    Scheduled = 2,
    InProgress = 3,
    Reported = 4,
    Verified = 5,
    Cancelled = 6
}

public enum PrescriptionStatus
{
    Draft = 1,
    Submitted = 2,
    PartiallyDispensed = 3,
    Dispensed = 4,
    Cancelled = 5
}

public enum StockTransactionType
{
    Receipt = 1,
    DispenseOut = 2,
    IssueOut = 3,
    TransferOut = 4,
    TransferIn = 5,
    AdjustmentIncrease = 6,
    AdjustmentDecrease = 7,
    ReturnIn = 8
}

public enum AdmissionRequestStatus
{
    Requested = 1,
    Approved = 2,
    Admitted = 3,
    Cancelled = 4
}

public enum AdmissionStatus
{
    Active = 1,
    Discharged = 2,
    TransferredOut = 3,
    Cancelled = 4
}

public enum EmergencyTriageLevel
{
    Level1Immediate = 1,
    Level2Emergent = 2,
    Level3Urgent = 3,
    Level4LessUrgent = 4,
    Level5NonUrgent = 5
}

public enum SurgeryStatus
{
    Requested = 1,
    Scheduled = 2,
    Preparing = 3,
    InProgress = 4,
    Completed = 5,
    Cancelled = 6
}

public enum InvoiceStatus
{
    Draft = 1,
    Issued = 2,
    PartiallyPaid = 3,
    Paid = 4,
    Cancelled = 5,
    Refunded = 6
}

public enum PaymentMethod
{
    Cash = 1,
    BankTransfer = 2,
    Card = 3,
    EWalletSandbox = 4
}

public enum PaymentStatus
{
    Completed = 1,
    PartiallyRefunded = 2,
    Refunded = 3,
    Reversed = 4
}

public enum RefundStatus
{
    PendingApproval = 1,
    Approved = 2,
    Rejected = 3,
    Completed = 4
}

public enum PurchaseRequestStatus
{
    Draft = 1,
    Submitted = 2,
    Approved = 3,
    Ordered = 4,
    Received = 5,
    Rejected = 6
}

public enum IntegrationDirection
{
    Outbound = 1,
    Inbound = 2
}

public enum IntegrationStatus
{
    Pending = 1,
    Succeeded = 2,
    Failed = 3,
    DemoSimulated = 4
}
