namespace SmartHospital.Domain.Common;

public static class RoleNames
{
    public const string SystemAdmin = "SystemAdmin";
    public const string HospitalAdmin = "HospitalAdmin";
    public const string HospitalDirector = "HospitalDirector";
    public const string DepartmentHead = "DepartmentHead";
    public const string Receptionist = "Receptionist";
    public const string CustomerService = "CustomerService";
    public const string Doctor = "Doctor";
    public const string Nurse = "Nurse";
    public const string NurseManager = "NurseManager";
    public const string LabTechnician = "LabTechnician";
    public const string LabReviewer = "LabReviewer";
    public const string RadiologyTechnician = "RadiologyTechnician";
    public const string Radiologist = "Radiologist";
    public const string Pharmacist = "Pharmacist";
    public const string PharmacyManager = "PharmacyManager";
    public const string Cashier = "Cashier";
    public const string Accountant = "Accountant";
    public const string InventoryStaff = "InventoryStaff";
    public const string HRStaff = "HRStaff";
    public const string Auditor = "Auditor";
    public const string Patient = "Patient";

    public static readonly IReadOnlyList<(string Code, string DisplayName, string Description)> All =
    [
        (SystemAdmin, "Quản trị hệ thống", "Quản trị kỹ thuật toàn hệ thống"),
        (HospitalAdmin, "Quản trị bệnh viện", "Quản trị danh mục, người dùng, cấu hình bệnh viện"),
        (HospitalDirector, "Ban giám đốc", "Theo dõi điều hành và báo cáo bệnh viện"),
        (DepartmentHead, "Trưởng khoa/phòng", "Quản lý hoạt động trong khoa/phòng được phân công"),
        (Receptionist, "Nhân viên tiếp đón", "Tiếp nhận, đăng ký khám, cấp số thứ tự"),
        (CustomerService, "Chăm sóc khách hàng", "Hỗ trợ lịch hẹn và thông tin bệnh nhân"),
        (Doctor, "Bác sĩ", "Khám bệnh, ghi bệnh án, chỉ định, kê đơn"),
        (Nurse, "Điều dưỡng", "Theo dõi sinh hiệu, chăm sóc, thực hiện y lệnh"),
        (NurseManager, "Điều dưỡng trưởng", "Điều phối điều dưỡng và giường bệnh"),
        (LabTechnician, "Kỹ thuật viên xét nghiệm", "Lấy mẫu, nhận mẫu, nhập kết quả xét nghiệm"),
        (LabReviewer, "Người duyệt kết quả XN", "Kiểm tra, duyệt và đính chính kết quả xét nghiệm"),
        (RadiologyTechnician, "Kỹ thuật viên CĐHA", "Tiếp nhận chỉ định và thực hiện chụp CĐHA"),
        (Radiologist, "Bác sĩ CĐHA", "Đọc và duyệt kết quả chẩn đoán hình ảnh"),
        (Pharmacist, "Dược sĩ", "Kiểm tra đơn, cấp phát thuốc"),
        (PharmacyManager, "Quản lý dược", "Quản lý kho dược, nhập xuất, điều chỉnh tồn"),
        (Cashier, "Thu ngân", "Lập bảng kê, thu tiền, in phiếu thu"),
        (Accountant, "Kế toán", "Bảng giá, đối soát doanh thu, phê duyệt hoàn tiền/điều chỉnh"),
        (InventoryStaff, "Nhân viên kho vật tư", "Quản lý kho vật tư, nhập xuất, kiểm kê"),
        (HRStaff, "Nhân viên nhân sự", "Quản lý hồ sơ nhân viên, ca trực"),
        (Auditor, "Kiểm toán nội bộ", "Chỉ đọc nhật ký audit và báo cáo"),
        (Patient, "Bệnh nhân", "Truy cập cổng thông tin bệnh nhân")
    ];
}

public static class PermissionCodes
{
    // Admin & Security
    public const string AdminUserManage = "admin.user.manage";
    public const string AdminRoleManage = "admin.role.manage";
    public const string AdminSettingsManage = "admin.settings.manage";
    public const string AdminCatalogManage = "admin.catalog.manage";
    public const string AdminSystemStatusRead = "admin.system.read";
    public const string AuditLogRead = "audit.log.read";
    public const string IntegrationRead = "integration.message.read";

    // Patient
    public const string PatientRead = "patient.record.read";
    public const string PatientCreate = "patient.record.create";
    public const string PatientUpdate = "patient.record.update";
    public const string PatientMerge = "patient.record.merge";

    // Reception & Appointment
    public const string AppointmentRead = "reception.appointment.read";
    public const string AppointmentManage = "reception.appointment.manage";
    public const string EncounterStart = "reception.encounter.start";
    public const string QueueManage = "reception.queue.manage";

    // Clinical EMR
    public const string ClinicalRead = "clinical.emr.read";
    public const string ClinicalWrite = "clinical.emr.write";
    public const string ClinicalNoteSign = "clinical.note.sign";
    public const string ClinicalNoteAmend = "clinical.note.amend";
    public const string ClinicalOrderCreate = "clinical.order.create";
    public const string PrescriptionCreate = "clinical.prescription.create";

    // Laboratory
    public const string LabRead = "lab.order.read";
    public const string LabSpecimenManage = "lab.specimen.manage";
    public const string LabResultEnter = "lab.result.enter";
    public const string LabResultVerify = "lab.result.verify";
    public const string LabResultAmend = "lab.result.amend";

    // Imaging
    public const string ImagingRead = "imaging.order.read";
    public const string ImagingPerform = "imaging.order.perform";
    public const string ImagingReportVerify = "imaging.report.verify";

    // Pharmacy & Inventory
    public const string PharmacyRead = "pharmacy.stock.read";
    public const string PharmacyDispense = "pharmacy.dispense.execute";
    public const string PharmacyStockAdjust = "pharmacy.stock.adjust";
    public const string InventoryManage = "inventory.stock.manage";

    // Inpatient, Nursing, Emergency, Surgery
    public const string InpatientRead = "inpatient.admission.read";
    public const string InpatientManage = "inpatient.admission.manage";
    public const string NursingRead = "nursing.care.read";
    public const string NursingWrite = "nursing.care.write";
    public const string EmergencyManage = "emergency.intake.manage";
    public const string SurgeryRead = "surgery.schedule.read";
    public const string SurgeryManage = "surgery.schedule.manage";

    // Billing & Insurance
    public const string BillingRead = "billing.invoice.read";
    public const string BillingChargeManage = "billing.charge.manage";
    public const string BillingPaymentCollect = "billing.payment.collect";
    public const string BillingRefundApprove = "billing.refund.approve";
    public const string BillingPriceManage = "billing.price.manage";

    // HR & Reports & Portal
    public const string HrManage = "hr.employee.manage";
    public const string ReportRead = "report.dashboard.read";
    public const string PortalAccess = "portal.patient.access";

    public static readonly IReadOnlyList<(string Code, string Module, string Name)> All =
    [
        (AdminUserManage, "Admin", "Quản lý người dùng và phạm vi dữ liệu"),
        (AdminRoleManage, "Admin", "Quản lý vai trò và phân quyền"),
        (AdminSettingsManage, "Admin", "Cấu hình hệ thống"),
        (AdminCatalogManage, "Admin", "Quản lý danh mục bệnh viện, khoa, phòng, dịch vụ"),
        (AdminSystemStatusRead, "Admin", "Xem trạng thái kỹ thuật hệ thống"),
        (AuditLogRead, "Audit", "Xem nhật ký audit hệ thống"),
        (IntegrationRead, "Integration", "Xem thông điệp tích hợp"),

        (PatientRead, "Patient", "Xem hồ sơ bệnh nhân"),
        (PatientCreate, "Patient", "Tạo hồ sơ bệnh nhân"),
        (PatientUpdate, "Patient", "Cập nhật hồ sơ bệnh nhân"),
        (PatientMerge, "Patient", "Hợp nhất hồ sơ bệnh nhân trùng lặp"),

        (AppointmentRead, "Reception", "Xem lịch hẹn và hàng đợi"),
        (AppointmentManage, "Reception", "Tạo và cập nhật lịch hẹn"),
        (EncounterStart, "Reception", "Tiếp nhận và tạo lượt khám"),
        (QueueManage, "Reception", "Điều phối số thứ tự và hàng đợi"),

        (ClinicalRead, "Clinical", "Xem hồ sơ bệnh án ngoại/nội trú"),
        (ClinicalWrite, "Clinical", "Ghi nhận sinh hiệu, khám bệnh, chẩn đoán"),
        (ClinicalNoteSign, "Clinical", "Ký xác nhận phiếu khám/bệnh án"),
        (ClinicalNoteAmend, "Clinical", "Bổ sung/đính chính bệnh án đã ký"),
        (ClinicalOrderCreate, "Clinical", "Chỉ định dịch vụ, XN, CĐHA"),
        (PrescriptionCreate, "Clinical", "Kê đơn thuốc"),

        (LabRead, "Laboratory", "Xem danh sách chỉ định xét nghiệm"),
        (LabSpecimenManage, "Laboratory", "Lấy mẫu, nhận mẫu, từ chối mẫu"),
        (LabResultEnter, "Laboratory", "Nhập kết quả xét nghiệm"),
        (LabResultVerify, "Laboratory", "Duyệt kết quả xét nghiệm"),
        (LabResultAmend, "Laboratory", "Đính chính kết quả xét nghiệm đã duyệt"),

        (ImagingRead, "Imaging", "Xem chỉ định CĐHA"),
        (ImagingPerform, "Imaging", "Thực hiện chụp và nhập mô tả CĐHA"),
        (ImagingReportVerify, "Imaging", "Duyệt kết quả CĐHA"),

        (PharmacyRead, "Pharmacy", "Xem tồn kho thuốc và đơn chờ cấp"),
        (PharmacyDispense, "Pharmacy", "Duyệt và cấp phát thuốc"),
        (PharmacyStockAdjust, "Pharmacy", "Nhập/xuất/điều chỉnh tồn kho dược"),
        (InventoryManage, "Inventory", "Quản lý kho vật tư và đề xuất mua hàng"),

        (InpatientRead, "Inpatient", "Xem danh sách nội trú và sơ đồ giường"),
        (InpatientManage, "Inpatient", "Tiếp nhận nhập viện, phân/chuyển giường, xuất viện"),
        (NursingRead, "Nursing", "Xem hồ sơ chăm sóc điều dưỡng"),
        (NursingWrite, "Nursing", "Ghi nhận sinh hiệu, chăm sóc, thực hiện thuốc"),
        (EmergencyManage, "Emergency", "Tiếp nhận và phân loại cấp cứu"),
        (SurgeryRead, "Surgery", "Xem lịch phẫu thuật"),
        (SurgeryManage, "Surgery", "Lập kế hoạch và ghi nhận phẫu thuật"),

        (BillingRead, "Billing", "Xem hóa đơn, công nợ, bảng kê"),
        (BillingChargeManage, "Billing", "Ghi nhận và chỉnh sửa khoản thu chưa chốt"),
        (BillingPaymentCollect, "Billing", "Thu tiền và lập phiếu thu"),
        (BillingRefundApprove, "Billing", "Phê duyệt hoàn tiền và điều chỉnh tài chính"),
        (BillingPriceManage, "Billing", "Quản lý bảng giá dịch vụ"),

        (HrManage, "HR", "Quản lý nhân viên và ca trực"),
        (ReportRead, "Reports", "Xem dashboard và báo cáo hoạt động"),
        (PortalAccess, "Portal", "Truy cập cổng bệnh nhân")
    ];
}
