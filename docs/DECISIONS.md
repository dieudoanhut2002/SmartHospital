# Các quyết định kiến trúc & nghiệp vụ (DECISIONS)

## ADR-001: Modular Monolith với .NET 10 LTS & Razor MVC + API v1
- **Quyết định:** Sử dụng 1 ASP.NET Core Web application (`SmartHospital.Web`) với MVC Areas cho 16 phân hệ UI và `Api/v1` Controllers cho REST API, tách lớp qua `Domain`, `Contracts`, `Application`, `Infrastructure`.
- **Lý do:** Đáp ứng Mục 3 và Mục 5.2 của đặc tả, dễ triển khai single-instance trên Windows/SQL Server LocalDB nhưng vẫn tách biệt ranh giới module và abstraction rõ ràng.

## ADR-002: Tách Domain khỏi ASP.NET Core Identity EF Stores
- **Quyết định:** `SmartHospital.Domain` chỉ chứa POCO entities, enums, domain rules (không tham chiếu `Microsoft.AspNetCore.Identity.EntityFrameworkCore`). `ApplicationUser` và `ApplicationRole` nằm ở `SmartHospital.Infrastructure.Identity`, liên kết với `Permission`, `RolePermission`, `UserDepartmentScope`, `UserFacilityScope` bằng `Guid UserId` / `Guid RoleId`.
- **Lý do:** Giữ `Domain` thuần khiết, không phụ thuộc Infrastructure hay EF Identity, đồng thời tận dụng đầy đủ password hashing, lockout, security stamp của ASP.NET Core Identity.

## ADR-003: Định danh khóa chính nhất quán
- **Quyết định:** Sử dụng `Guid` (`uniqueidentifier`) cho các entity nghiệp vụ và `long` (`bigint identity`) cho bảng log khối lượng lớn (`AuditLogs`, `IntegrationMessages`, `QueueTickets` vẫn có `Guid Id` + `TicketNumber`).
- **Lý do:** Phù hợp khuyến nghị Mục 7.10, thuận lợi mở rộng đồng bộ đa cơ sở mà vẫn tối ưu dung lượng cho bảng audit/log.

## ADR-004: Khởi tạo Database an toàn và chống chạy migration đồng thời
- **Quyết định:** `DatabaseInitializer` chạy tại startup trước khi nhận HTTP request (gọi trong `Program.cs` trước `app.Run()`), kiểm tra connection string, mở kết nối SQL Server, lấy SQL Server application lock (`sp_getapplock` trên `SmartHospital_Migration_Lock`), chạy `db.Database.MigrateAsync()` khi `Database:AutoMigrate=true`, và chạy `HospitalDataSeeder.SeedAsync()` khi `Database:SeedInitialData=true`.
- **Lý do:** Đảm bảo database tự khởi tạo trên máy mới mà không dùng `EnsureCreated()`, không ghi đè dữ liệu hiện có, báo lỗi rõ khi SQL Server/LocalDB chưa sẵn sàng, và an toàn nếu nhiều tiến trình khởi động cùng lúc.

## ADR-005: Tồn kho dựa trên Ledger (`StockTransaction`)
- **Quyết định:** Mọi thay đổi tồn kho thuốc/vật tư (nhập, xuất cấp phát, điều chuyển, điều chỉnh kiểm kê) đều phải đi qua `StockTransaction` + `StockTransactionItem`. Bảng `MedicineBatch` và `InventoryBatch` lưu `QuantityOnHand` được cập nhật trong cùng transaction với kiểm tra ràng buộc `QuantityOnHand >= 0` và `RowVersion` optimistic concurrency.
- **Lý do:** Đáp ứng Quy tắc 6 & 7 trong Mục 8 (không sửa trực tiếp tồn kho rời rạc, luôn có lịch sử giao dịch đối chiếu được và tránh race condition khi cấp phát đồng thời).
