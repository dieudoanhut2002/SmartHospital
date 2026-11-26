# SmartHospital — Kế hoạch triển khai chi tiết (Implementation Plan)

Tài liệu này lập kế hoạch triển khai theo 11 phase bắt buộc trong Mục 14 của `SMARTHOSPITAL_MASTER_SPEC.md`.

## Phase 0 — Repository & Engineering Foundation
- Kiểm tra workspace, .NET SDK (`.NET 10.0.403`), SQL Server LocalDB (`MSSQLLocalDB`), `dotnet-ef`.
- Khởi tạo `.gitignore`, `.editorconfig`, `Directory.Build.props` (Nullable, Analyzers, `net10.0`).
- Tạo `SmartHospital.sln` và các project:
  - `src/SmartHospital.Domain`
  - `src/SmartHospital.Contracts`
  - `src/SmartHospital.Application`
  - `src/SmartHospital.Infrastructure`
  - `src/SmartHospital.Web`
  - `tests/SmartHospital.UnitTests`
  - `tests/SmartHospital.IntegrationTests`
- Thiết lập tài liệu trạng thái ban đầu và script build/test (`scripts/build-and-test.ps1`, `scripts/publish-release.ps1`).
- **Đầu ra:** Solution restore và build thành công không lỗi.

## Phase 1 — SQL Server & Identity Foundation
- Thiết kế toàn bộ Domain Entities & Enums cho các phân hệ cốt lõi, cấu hình EF Core (`HospitalDbContext`, `IEntityTypeConfiguration<T>`), AuditLog, SystemSettings.
- Tạo Initial EF Core Migration (`InitialCreate`).
- Xây dựng `DatabaseInitializer` (kiểm tra connection string, SQL Server connectivity, `sp_getapplock`, `MigrateAsync`, bảo vệ không xóa/reset dữ liệu, thông báo lỗi tiếng Việt rõ ràng).
- Xây dựng `HospitalDataSeeder` idempotent (Bệnh viện, Cơ sở, Khoa/Phòng, Chuyên khoa, Phòng khám, Giường, Dịch vụ, Thuốc mẫu, 21 Roles, Permissions, RolePermissions, tài khoản `admin` với `MustChangePassword = true` và password sinh từ config/ngẫu nhiên an toàn).
- Cấu hình ASP.NET Core Identity, Cookie HttpOnly/SameSite, Rate limiting, Lockout, `MustChangePasswordMiddleware`, `CorrelationIdMiddleware`, `GlobalExceptionHandlerMiddleware`, Permission & Scope Authorization.
- Xây dựng màn hình Login, Logout, ChangePassword, AccessDenied, Error, HealthChecks, và khu vực `Admin` (Users, Roles, Permissions, Scopes, SystemSettings, SystemStatus, AuditLogs).
- **Đầu ra:** Ứng dụng tự tạo database `SmartHospitalDb` trên LocalDB khi chạy lần đầu, đăng nhập/đổi mật khẩu và phân quyền hoạt động.

## Phase 2 — Master Data & Patient
- Quản lý Cơ sở (`Facility`), Khoa/Phòng (`Department`), Chuyên khoa (`Specialty`), Phòng (`Room`), Phòng khám (`Clinic`), Giường (`Bed`), Dịch vụ (`ServiceCatalog`).
- Quản lý Bệnh nhân (`Patient`, `PatientIdentifier`, `PatientContact`, `PatientEmergencyContact`, `PatientAllergy`, `PatientAlert`, `PatientMergeHistory`).
- Tìm kiếm phân trang đa tiêu chí, cảnh báo trùng lặp hồ sơ (họ tên + ngày sinh / SĐT / giấy tờ định danh), hợp nhất hồ sơ có audit.
- **Đầu ra:** Quản lý danh mục và hồ sơ bệnh nhân hoàn chỉnh.

## Phase 3 — Reception & Appointment
- Đặt lịch khám (`Appointment`), Tiếp đón trực tiếp (`Walk-in`), Lượt khám (`Encounter`, `EncounterStatusHistory`).
- Cấp số thứ tự (`QueueTicket`) duy nhất theo ngày/phòng khám/quầy, Bảng hàng đợi (check-in, gọi số, bỏ lượt, chuyển hàng đợi).
- Mẫu in Phiếu tiếp nhận & Phiếu số thứ tự.
- **Đầu ra:** Quy trình tiếp nhận, xếp hàng và tạo lượt khám thông suốt.

## Phase 4 — Clinical EMR
- Workspace khám bệnh cho bác sĩ theo phòng khám/khoa được phân quyền.
- Ghi nhận Sinh hiệu (`VitalSign`), Ghi chú lâm sàng (`ClinicalNote` + `ClinicalNoteVersion`), Chẩn đoán (`DiagnosisCatalog`, `EncounterDiagnosis`).
- Chỉ định dịch vụ (`ServiceOrder`), xét nghiệm (`LabOrder`), chẩn đoán hình ảnh (`ImagingOrder`), kê đơn thuốc (`Prescription`, `PrescriptionItem`).
- Quy tắc ký/khóa bệnh án và tạo bản bổ sung/đính chính (`Amendment`) giữ nguyên phiên bản cũ và audit.
- Mẫu in Phiếu khám, Phiếu chỉ định, Đơn thuốc.
- **Đầu ra:** Hoàn thành quy trình khám ngoại trú.

## Phase 5 — LIS & Imaging Basic
- Danh mục xét nghiệm (`LabTest`) & dịch vụ CĐHA (`ImagingService`).
- LIS worklist: nhận chỉ định, sinh mã mẫu/barcode (`Specimen`), lấy mẫu/nhận mẫu/từ chối mẫu, nhập kết quả (`LabResult`), kiểm tra ngưỡng tham chiếu cấu hình, duyệt & khóa phiên bản (`Verified`), sửa kết quả tạo `LabResultVersion`.
- RIS/PACS: tiếp nhận chỉ định (`ImagingOrder`), ghi nhận kết quả (`ImagingReport`), đính kèm tài liệu an toàn qua `IDocumentStorage`, adapter `Demo`/`Sandbox` cho `ILabDeviceIntegration`, `IRadiologyIntegration`, `IPacsIntegration`.
- Trả kết quả về EMR và in Phiếu kết quả xét nghiệm.
- **Đầu ra:** Quy trình cận lâm sàng từ chỉ định đến trả kết quả.

## Phase 6 — Pharmacy & Inventory
- Danh mục thuốc (`Medicine`, `MedicineUnit`), Kho (`Warehouse`, `WarehouseLocation`), Lô thuốc (`MedicineBatch`), Nhà cung cấp (`Supplier`), Vật tư (`InventoryItem`, `InventoryBatch`), Đề xuất mua hàng (`PurchaseRequest`).
- Nhập kho, xuất kho, điều chuyển, kiểm kê/điều chỉnh có lý do qua `StockTransaction` & `StockTransactionItem` (không cho xuất quá tồn, cảnh báo hết hạn/tồn tối thiểu).
- Duyệt và cấp phát đơn thuốc ngoại trú/nội trú (`PrescriptionDispensing`), ghi nhận số lượng thực cấp và tự động tạo phiếu xuất kho + khoản phí.
- **Đầu ra:** Quy trình kê đơn, cấp phát và quản lý tồn kho.

## Phase 7 — Billing & Insurance
- Bảng giá dịch vụ (`ServicePriceList`, `ServicePrice`) theo thời gian hiệu lực, snapshot đơn giá khi phát sinh.
- Tự động/thủ công tổng hợp khoản phí vào Hóa đơn (`Invoice`, `InvoiceItem`), hỗ trợ nhiều phương thức thanh toán, tạm ứng, thanh toán nhiều lần (`Payment`, `PaymentAllocation`), không cho phân bổ vượt số dư.
- Hoàn tiền (`Refund`) tham chiếu payment gốc, điều chỉnh (`BillingAdjustment`) yêu cầu lý do và quyền phê duyệt.
- Hồ sơ BHYT (`InsuranceProfile`, `InsuranceClaim` staging) và adapter `Demo`/`Sandbox` cho `IInsuranceIntegration`, `IEInvoiceIntegration`, `IPaymentGateway`.
- Báo cáo thu ngân & mẫu in Bảng kê chi phí, Phiếu thu.
- **Đầu ra:** Quy trình tài chính/thanh toán xuyên suốt.

## Phase 8 — Inpatient, Nursing & Emergency
- Tiếp nhận Cấp cứu (`EmergencyIntake`) với phân loại ưu tiên do nhân viên y tế nhập.
- Đề nghị nhập viện (`AdmissionRequest`), Đợt nội trú (`Admission`), Bảng giường (`BedBoard`), Phân giường/chuyển giường (`BedAssignment`, `BedStatusHistory`) chống trùng hiệu lực giường, quy trình xuất viện & chuyển giường sang `Cleaning` -> `Available`.
- Phân hệ Điều dưỡng (`NursingAssignment`, `NursingNote`, `NursingObservation`, `MedicationAdministration`, `ShiftHandoverNote`).
- Mẫu in Tóm tắt ra viện.
- **Đầu ra:** Quản lý cấp cứu, nội trú và điều dưỡng cơ bản.

## Phase 9 — Surgery, Patient Portal & Reports
- Lập kế hoạch phẫu thuật (`SurgeryRequest`, `SurgerySchedule`, `SurgeryRecord`), ekip, checklist cấu hình, vật tư sử dụng.
- Cổng bệnh nhân (`Patient Portal`): xem lịch hẹn, đặt lịch hẹn, xem kết quả đã công bố, đơn thuốc, hóa đơn/công nợ, cập nhật liên hệ (tách biệt hoàn toàn khỏi quyền sửa EMR).
- Dashboard theo vai trò và các báo cáo lọc theo ngày/cơ sở/khoa.
- Notification adapter demo (`INotificationProvider`) & màn hình theo dõi `IntegrationMessages`.
- Hoàn thiện toàn bộ 8 mẫu in HTML/CSS.
- **Đầu ra:** Đầy đủ tất cả phân hệ theo đặc tả.

## Phase 10 — Hardening, Testing & Release
- Rà soát bảo mật (CSRF, XSS encoding, SQL parameterization, rate limiting, authorization scope ở server, audit log, file upload safety, masking secrets).
- Hoàn thiện tài liệu kiến trúc & ERD (`docs/ARCHITECTURE-ERD.md`).
- Viết và chạy đầy đủ Unit Tests (`SmartHospital.UnitTests`), Integration Tests (`SmartHospital.IntegrationTests` trên SQL Server LocalDB thật), và E2E Smoke Tests (qua `WebApplicationFactory` + LocalDB thật).
- Kiểm tra Database Bootstrap trên database rỗng, chạy lại seed idempotent, test override connection string bằng biến môi trường, publish Release (`dotnet publish -c Release`) và kiểm tra chạy bản publish.
- Hoàn thiện `README.md` và cập nhật toàn bộ `docs/*.md`.
