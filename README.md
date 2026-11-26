# SmartHospital — Hệ thống Quản lý Bệnh viện Tích hợp

**SmartHospital** là nền tảng quản lý hoạt động bệnh viện đa khoa theo kiến trúc Modular Monolith trên **.NET 10 LTS**, **ASP.NET Core MVC/Razor + REST API v1**, **Entity Framework Core 10** và **Microsoft SQL Server** (mặc định **SQL Server LocalDB** trên Windows).

> **Lưu ý an toàn lâm sàng & pháp lý:** Đây là phần mềm quản lý quy trình và thông tin y tế. Hệ thống **không** tự động đưa ra chẩn đoán, phác đồ hay quyết định điều trị. Các tích hợp ngoài (LIS máy xét nghiệm, RIS/PACS, BHYT, Hóa đơn điện tử, Cổng thanh toán) trong bản MVP sử dụng **Adapter Demo/Sandbox** và cần cấu hình, kiểm định chuyên môn/pháp lý trước khi vận hành thực tế.

---

## 1. Tính năng đã triển khai

| Phân hệ | Trạng thái | Chức năng chính |
|---|---|---|
| **Quản trị & Bảo mật (`Admin`)** | Đã triển khai | Quản lý User, 21 Vai trò, Ma trận quyền RBAC, Phạm vi Cơ sở/Khoa, Danh mục, SystemSettings, AuditLogs (chỉ đọc), IntegrationMessages, Trạng thái hệ thống |
| **Quản lý Bệnh nhân (`Patients`)** | Đã triển khai | Tạo/sửa/tìm kiếm phân trang, Cảnh báo trùng lặp hồ sơ, Dị ứng & Cảnh báo quan trọng, Hợp nhất hồ sơ có audit |
| **Tiếp đón & Lịch hẹn (`Reception`)** | Đã triển khai | Đặt lịch hẹn, Check-in / Walk-in tạo `Encounter`, Cấp số thứ tự duy nhất theo phòng khám/ngày, Bảng hàng đợi (gọi số, vào khám, chuyển phòng, bỏ lượt) |
| **Khám bệnh (`Clinical EMR`)** | Đã triển khai | Worklist theo phạm vi, Sinh hiệu, Phiếu khám (Nháp / Ký khóa / Bổ sung đính chính giữ lịch sử phiên bản), Chẩn đoán ICD, Chỉ định DV/XN/CĐHA, Kê đơn thuốc, Chỉ định nhập viện |
| **Xét nghiệm (`LIS`) & CĐHA (`RIS/PACS`)** | Đã triển khai | Lấy mẫu/nhận mẫu/từ chối mẫu với Barcode, Nhập kết quả, Tự đánh dấu bất thường theo ngưỡng cấu hình, Duyệt khóa & Đính chính phiên bản, Upload tài liệu ngoài webroot, Adapter Demo/Sandbox |
| **Dược & Vật tư (`Pharmacy & Inventory`)** | Đã triển khai | Nhập lô thuốc, Xuất cấp phát theo FEFO, Chặn xuất âm tồn kho, Cảnh báo tồn thấp/sắp hết hạn/hết hạn, Sổ giao dịch kho, Đề xuất mua hàng |
| **Viện phí (`Billing & Insurance`)** | Đã triển khai | Tổng hợp khoản thu chưa lập hóa đơn, Snapshot đơn giá, Thu tiền nhiều phương thức & Tạm ứng, Chặn thu quá số dư, Hoàn tiền có lý do & giới hạn, Staging BHYT & HĐĐT Demo, Báo cáo thu ngân |
| **Nội trú, Điều dưỡng, Cấp cứu, Phẫu thuật** | Đã triển khai | Tiếp nhận cấp cứu & phân loại ưu tiên, Sơ đồ giường (`Available`/`Occupied`/`Cleaning`/`Maintenance`), Chống trùng giường ở DB, Chuyển giường, Xuất viện, Phiếu chăm sóc/Sinh hiệu/MAR điều dưỡng, Lịch mổ |
| **Nhân sự (`HR`), Cổng bệnh nhân (`Portal`), Báo cáo (`Reports`)** | Đã triển khai | Hồ sơ nhân viên & Ca trực, Cổng bệnh nhân đặt lịch/xem đơn/hóa đơn/kết quả công bố, Dashboard KPI theo quyền & Báo cáo lọc theo thời gian/cơ sở |
| **In ấn (`Print`)** | Đã triển khai | 6 route / 8 mẫu in HTML/CSS print-friendly (Phiếu tiếp nhận, STT, Phiếu khám & Chỉ định, KQ Xét nghiệm, Đơn thuốc, Bảng kê & Phiếu thu, Tóm tắt ra viện) |

---

## 2. Yêu cầu môi trường (Prerequisites)

1. **Hệ điều hành:** Windows 10/11 hoặc Windows Server.
2. **.NET 10 SDK** (để build/phát triển) hoặc **ASP.NET Core Runtime 10.0** (để chạy bản publish framework-dependent).
   - Kiểm tra: `dotnet --info`
3. **Microsoft SQL Server:**
   - Phát triển local: **SQL Server Express LocalDB** (đi kèm Visual Studio hoặc bộ cài SqlLocalDB).
   - Hoặc **SQL Server Express / Standard / Enterprise**.

---

## 3. Cấu hình Database & Connection String

Cấu hình mặc định trong `src/SmartHospital.Web/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SmartHospitalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Database": {
    "AutoMigrate": true,
    "SeedInitialData": true,
    "SeedDemoData": false
  }
}
```

### Đổi Connection String qua Biến môi trường (không sửa source code)
```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost\SQLEXPRESS;Database=SmartHospitalDb;Trusted_Connection=True;TrustServerCertificate=True"
$env:Database__AutoMigrate = "true"
$env:Database__SeedInitialData = "true"
```

### Quyền SQL Server & Tự khởi tạo Database
- Khi khởi động (`DatabaseInitializer.InitializeDatabaseAsync`), nếu database chưa tồn tại và `Database:AutoMigrate = true`, ứng dụng tự tạo database trên SQL Server và áp dụng toàn bộ EF Core Migrations (có bảo vệ đồng thời bằng `sp_getapplock`).
- Nếu database đã tồn tại, ứng dụng **không** xóa hay ghi đè dữ liệu; chỉ áp dụng các migration còn thiếu.
- **Lưu ý Production:** Tài khoản kết nối cần quyền `CREATE DATABASE` trong lần chạy đầu, hoặc DBA tạo sẵn database trống và đặt `Database:AutoMigrate = false` rồi chạy migration thủ công.

---

## 4. Hướng dẫn chạy Development

```powershell
# 1. Khởi động SQL Server LocalDB (nếu chưa chạy)
sqllocaldb start MSSQLLocalDB

# 2. Restore và Build
dotnet restore SmartHospital.sln
dotnet build SmartHospital.sln

# 3. Chạy ứng dụng Web (tự tạo SmartHospitalDb + Seed dữ liệu)
dotnet run --project src/SmartHospital.Web/SmartHospital.Web.csproj
```

### Tài khoản đăng nhập lần đầu
1. **Bootstrap Admin (`admin`):**
   - Trong môi trường Development, nếu chưa cấu hình `BootstrapAdmin:Password`, hệ thống tự sinh một mật khẩu mạnh ngẫu nhiên và in **một lần** trong console log khi khởi tạo DB lần đầu.
   - Bạn có thể đặt trước qua biến môi trường: `$env:BootstrapAdmin__Password = "YourStrongPassword@2026!"`.
   - Tài khoản `admin` **bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên**.
   - Nếu tài khoản `admin` đã tồn tại, các lần khởi động lại sẽ **không** reset mật khẩu.
2. **Tài khoản Demo (chỉ tạo khi `Database:SeedDemoData = true`, mặc định bật ở Development):**
   - Mật khẩu mặc định (Development): `Demo@123456!` (có thể đổi qua `BootstrapAdmin:DemoUserPassword`).
   - Các tài khoản mẫu: `reception.demo`, `doctor.demo`, `nurse.demo`, `lab.demo`, `labreviewer.demo`, `radiology.demo`, `pharmacist.demo`, `cashier.demo`, `patient.demo`.

---

## 5. Chạy EF Core Migration thủ công

```powershell
# Tạo migration mới khi thay đổi Domain/DbContext
dotnet ef migrations add <TenMigration> --project src/SmartHospital.Infrastructure --startup-project src/SmartHospital.Web --output-dir Persistence/Migrations

# Cập nhật database thủ công
dotnet ef database update --project src/SmartHospital.Infrastructure --startup-project src/SmartHospital.Web
```

---

## 6. Kiểm thử (Unit Tests & Integration Tests trên SQL Server thật)

```powershell
# Chạy toàn bộ Unit Tests & Integration Tests (yêu cầu MSSQLLocalDB đang chạy)
.\scripts\build-and-test.ps1
```

---

## 7. Publish Release & Triển khai trên máy khác

```powershell
# Publish bản Release framework-dependent vào thư mục ./publish
.\scripts\publish-release.ps1

# Hoặc publish self-contained cho win-x64 (không cần cài sẵn .NET Runtime trên máy đích)
.\scripts\publish-release.ps1 -SelfContained
```

Trên máy đích:
1. Cài SQL Server LocalDB hoặc SQL Server/SQL Express.
2. Chỉnh sửa `appsettings.json` hoặc đặt biến môi trường `ConnectionStrings__DefaultConnection` và `BootstrapAdmin__Password`.
3. Chạy `SmartHospital.Web.exe` (hoặc `dotnet SmartHospital.Web.dll`). Ứng dụng sẽ tự tạo database nếu chưa có và mở cổng web theo cấu hình `ASPNETCORE_URLS`.

---

## 8. Backup & Restore

### Backup SQL Server Database
```sql
BACKUP DATABASE [SmartHospitalDb]
TO DISK = N'C:\Backups\SmartHospitalDb_Full.bak'
WITH FORMAT, INIT, NAME = N'SmartHospitalDb-Full Backup';
```

### Restore SQL Server Database
```sql
RESTORE DATABASE [SmartHospitalDb]
FROM DISK = N'C:\Backups\SmartHospitalDb_Full.bak'
WITH REPLACE, RECOVERY;
```

### Backup tệp đính kèm
Sao lưu định kỳ thư mục được cấu hình tại `Storage:RootPath` (mặc định `App_Data/Documents`) đồng bộ cùng lịch backup SQL Server.

---

## 9. Các Adapter Demo & Giới hạn Tích hợp thật
- `DemoLabDeviceIntegration`, `DemoRadiologyIntegration`, `DemoPacsIntegration`, `DemoInsuranceIntegration`, `DemoEInvoiceIntegration`, `DemoPaymentGateway`, `DemoNotificationProvider` chỉ ghi nhận thông điệp mô phỏng vào bảng `IntegrationMessages` để kiểm thử luồng nghiệp vụ.
- Chưa kết nối máy xét nghiệm vật lý (HL7/ASTM), PACS/DICOM server thật, cổng giám định BHYT Việt Nam, nhà cung cấp hóa đơn điện tử thật hoặc cổng thanh toán ngân hàng thật.

---

## 10. Troubleshooting SQL Server thường gặp
- **Lỗi `Cannot connect to (localdb)\MSSQLLocalDB`:** Chạy `sqllocaldb info MSSQLLocalDB` và `sqllocaldb start MSSQLLocalDB`. Nếu chưa có instance, chạy `sqllocaldb create MSSQLLocalDB -s`.
- **Lỗi `CREATE DATABASE permission denied`:** Cấp quyền `dbcreator` cho tài khoản SQL hoặc nhờ DBA tạo trước database và chạy migration thủ công.
- **Lỗi `Login failed for user`:** Kiểm tra lại `Trusted_Connection=True` (Windows Auth) hoặc `User Id`/`Password` trong connection string.
