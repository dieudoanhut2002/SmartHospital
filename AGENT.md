# AGENT.md — Hướng dẫn làm việc cho Agent

## Tổng quan dự án
- **Tên dự án:** SmartHospital — Hệ thống quản lý bệnh viện tích hợp.
- **Đặc tả gốc:** [SMARTHOSPITAL_MASTER_SPEC.md](SMARTHOSPITAL_MASTER_SPEC.md).
- **Ngôn ngữ hiển thị:** Tiếng Việt toàn bộ UI, validation message và tài liệu người dùng.
- **Ngôn ngữ mã nguồn:** Tiếng Anh cho class, method, property, database object, API contract.
- **Công nghệ:** .NET 10 LTS, ASP.NET Core MVC/Razor + REST API, EF Core 10, SQL Server (mặc định LocalDB), ASP.NET Core Identity, Bootstrap 5, xUnit.

## Nguyên tắc thực thi bắt buộc
1. **Kiến trúc Modular Monolith:**
   - `SmartHospital.Domain`: Entities, Value Objects, Enums, Constants, Domain Rules. Không phụ thuộc EF/Web/Infrastructure.
   - `SmartHospital.Contracts`: DTOs, API request/response contracts, PagedResult.
   - `SmartHospital.Application`: Use-case services, `IHospitalDbContext`, integration interfaces, authorization/audit abstractions.
   - `SmartHospital.Infrastructure`: `HospitalDbContext`, Entity Configurations, EF Core Migrations, `DatabaseInitializer`, `HospitalDataSeeder`, ASP.NET Core Identity implementation, Local file storage, Demo/Sandbox adapters.
   - `SmartHospital.Web`: MVC Areas, Controllers, Views, API v1 Controllers, Middleware, Security & Authorization policies, Print templates.
2. **Toàn vẹn dữ liệu & An toàn y tế:**
   - Không tự động đưa ra chẩn đoán, phác đồ hay phân loại cấp cứu AI.
   - Hồ sơ đã ký (`ClinicalNote`), kết quả xét nghiệm đã duyệt (`LabResult`), giao dịch tài chính (`Invoice`/`Payment`/`Refund`) và giao dịch kho (`StockTransaction`) không được sửa/xóa âm thầm; phải tạo phiên bản/đính chính hoặc giao dịch đảo/hoàn có lý do và audit.
   - Tồn kho tính qua `StockTransaction` / ledger, không cho xuất quá số lượng tồn.
   - Không dùng `EnsureCreated()` hoặc `EnsureDeleted()`. Mọi schema đi qua EF Core Migrations và `DatabaseInitializer`.
3. **Quy trình làm việc:**
   - Triển khai theo vertical slices từ Phase 0 đến Phase 10.
   - Build và test sau mỗi phase trước khi chuyển tiếp.
   - Cập nhật `docs/PROJECT-STATUS.md`, `docs/DEVELOPMENT-LOG.md`, `docs/NEXT-TASK.md` sau mỗi phase.
   - Ghi giả định vào `docs/ASSUMPTIONS.md`, quyết định vào `docs/DECISIONS.md`.
