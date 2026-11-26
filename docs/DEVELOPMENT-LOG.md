# Nhật ký phát triển (DEVELOPMENT-LOG)

## 2026-11-26 — Triển khai hoàn chỉnh SmartHospital MVP (Phase 0 -> Phase 10)
- **Kiểm tra môi trường:** `.NET SDK 10.0.403`, `MSSQLLocalDB 15.0.4382.1` (Running), `dotnet-ef 10.0.13`.
- **Phase 0 & 1:** Khởi tạo `SmartHospital.sln` (7 projects: `Domain`, `Contracts`, `Application`, `Infrastructure`, `Web`, `UnitTests`, `IntegrationTests`), EF Core `HospitalDbContext`, migration `20261126070551_InitialCreate`, `DatabaseInitializer` với `sp_getapplock`, `HospitalDataSeeder` idempotent (21 Roles, 46+ Permissions, danh mục mẫu, bootstrap `admin` bắt buộc đổi mật khẩu), ASP.NET Core Identity, CSRF, Rate Limiting, Security Headers, CorrelationId & GlobalExceptionHandler.
- **Phase 2–9:** Triển khai toàn bộ Use Cases trong `SmartHospital.Application`, MVC Areas (`Admin`, `Patients`, `Reception`, `Clinical`, `Laboratory`, `Pharmacy`, `Billing`, `Inpatient`, `HR`, `Portal`, `Reports`), REST API `/api/v1/*` + Swagger Dev, và 6 view in ấn cho 8 mẫu in HTML/CSS.
- **Sửa lỗi phát hiện khi chạy test SQL Server thật:**
  1. Bỏ `EnableRetryOnFailure` mặc định trên `HospitalDbContext` để hỗ trợ user-initiated transactions (`BeginTransactionAsync`) xuyên suốt trong Application services.
  2. Chuyển `AddDbContext<HospitalDbContext>` sang overload `(sp, options)` để luôn đọc `IConfiguration` mới nhất (hỗ trợ override qua biến môi trường và `WebApplicationFactory`).
- **Kết quả kiểm thử thực tế (`dotnet test SmartHospital.sln --verbosity normal`):**
  - `SmartHospital.UnitTests`: **7/7 Passed** (0 Failed, 0 Skipped).
  - `SmartHospital.IntegrationTests` (trên SQL Server LocalDB `MSSQLLocalDB`): **2/2 Passed** (kiểm tra tự tạo DB rỗng, seed idempotent, không reset mật khẩu `admin`, luồng End-to-End từ Patient -> Appointment -> Encounter -> ClinicalNote Sign/Amend -> LabOrder/Verify -> Rx/Dispense -> Invoice/Payment/Refund -> Inpatient Admission/Transfer/Discharge -> AuditLogs, kiểm tra RBAC 403, và `AutoMigrate=false` báo lỗi rõ ràng).
- **Kết quả Publish & Clean-DB Bootstrap Smoke Test (`.\scripts\publish-release.ps1`):**
  - Publish Release thành công tại `C:\Git\SmartHospital\publish`.
  - Chạy `dotnet publish/SmartHospital.Web.dll` với `ConnectionStrings__DefaultConnection` trỏ tới database mới `SmartHospital_PublishSmoke_ae34b14c` và `ASPNETCORE_ENVIRONMENT=Production`: ứng dụng tự tạo database, chạy migration `InitialCreate`, seed dữ liệu khởi tạo và trả về `200 Healthy` tại `/health/ready`.

## 2026-11-26 (Cập nhật) — Bổ sung đầy đủ CRUD cho toàn bộ danh mục & dịch vụ
- Bổ sung đầy đủ Create/Read/Update/Soft-Delete (Toggle `IsActive`) trong `MasterDataService` và `Admin/Catalogs` (giao diện 6 tab) cho: Bệnh viện, Cơ sở, Khoa/Phòng, Chuyên khoa, Phòng, Phòng khám, Giường bệnh (chặn tắt giường đang có bệnh nhân), Dịch vụ khám & Thủ thuật, Bảng giá & Giá dịch vụ, Mã bệnh ICD-10, Xét nghiệm, CĐHA, Thuốc, Kho, Nhà cung cấp, Vật tư y tế.
- Bổ sung tạo vai trò tùy chỉnh trong `Admin/Roles`.
- Bổ sung CRUD chi tiết trên các phân hệ: Dị ứng/Cảnh báo/Liên hệ khẩn cấp/Thẻ bảo hiểm (`Patients`), Đổi lịch/Hủy lịch (`Reception`), Cập nhật trạng thái đề xuất mua hàng (`Pharmacy`), CRUD Nhân viên/Ca trực/Nghỉ phép (`HR`), Cập nhật liên hệ & Hủy lịch hẹn (`Portal`).
- Chạy `dotnet build SmartHospital.sln` (`0 Warning(s), 0 Error(s)`) và `dotnet test SmartHospital.sln --no-build`: `SmartHospital.UnitTests` **8/8 Passed**, `SmartHospital.IntegrationTests` **2/2 Passed**.
