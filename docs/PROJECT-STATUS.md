# Trạng thái dự án SmartHospital (PROJECT-STATUS)

**Cập nhật lần cuối:** 2026-11-26  
**Phiên bản hiện tại:** MVP 1.0.0 (.NET 10 LTS + SQL Server LocalDB)

## Tổng quan tiến độ theo Phase

| Phase | Nội dung | Trạng thái | Ghi chú |
|---|---|---|---|
| Phase 0 | Repository & Engineering Foundation | Hoàn thành | `.NET 10.0.403`, `SmartHospital.sln`, 7 projects, `Directory.Build.props`, scripts |
| Phase 1 | SQL Server & Identity Foundation | Hoàn thành | `HospitalDbContext`, `InitialCreate` migration, `DatabaseInitializer` (`sp_getapplock`), `HospitalDataSeeder`, Identity + RBAC + Scope Auth, Admin UI |
| Phase 2 | Master Data & Patient | Hoàn thành | Quản lý Cơ sở/Khoa/Phòng/Phòng khám/Giường/Dịch vụ, Hồ sơ bệnh nhân, cảnh báo trùng lặp, dị ứng, hợp nhất hồ sơ |
| Phase 3 | Reception & Appointment | Hoàn thành | Lịch hẹn, Walk-in/Check-in, `Encounter`, `QueueTicket` duy nhất theo phòng khám/ngày, Bảng hàng đợi |
| Phase 4 | Clinical EMR | Hoàn thành | Worklist, Sinh hiệu, `ClinicalNote` Nháp/Ký/Đính chính có phiên bản, Chẩn đoán ICD, Chỉ định DV/XN/CĐHA, Kê đơn thuốc |
| Phase 5 | LIS & Imaging Basic | Hoàn thành | Lấy/nhận/từ chối mẫu Barcode, Nhập/Duyệt/Đính chính KQ XN, Báo cáo CĐHA, upload file ngoài webroot, Adapter Demo/Sandbox |
| Phase 6 | Pharmacy & Inventory | Hoàn thành | Nhập lô thuốc, Xuất cấp phát FEFO qua `StockTransaction`, Chặn xuất âm tồn, Cảnh báo tồn thấp/HSD, Vật tư & Đề xuất mua hàng |
| Phase 7 | Billing & Insurance | Hoàn thành | Bảng giá, Tổng hợp khoản thu chưa lập HĐ, Thu tiền nhiều phương thức & Tạm ứng, Chặn thu quá số dư, Hoàn tiền có giới hạn & lý do, Staging BHYT/HĐĐT Demo |
| Phase 8 | Inpatient, Nursing & Emergency | Hoàn thành | Cấp cứu & Phân loại ưu tiên, Sơ đồ giường chống trùng hiệu lực, Chuyển giường, Xuất viện, Điều dưỡng (Notes/Obs/MAR) |
| Phase 9 | Surgery, Patient Portal & Reports | Hoàn thành | Lập kế hoạch phẫu thuật, Cổng bệnh nhân, Dashboard KPI & Báo cáo lọc ngày/cơ sở, 8 mẫu in HTML/CSS |
| Phase 10 | Hardening & Release | Hoàn thành | Unit Tests (7/7 pass), Integration Tests trên LocalDB (2/2 pass), Publish Release & Bootstrap smoke test (`200 Healthy`) |

## Các tích hợp ngoài ở chế độ Demo/Sandbox (chưa kết nối hệ thống thật)
- Máy xét nghiệm (`DemoLabDeviceIntegration`)
- RIS Modality Worklist & PACS Viewer (`DemoRadiologyIntegration`, `DemoPacsIntegration`)
- Giám định BHYT (`DemoInsuranceIntegration`)
- Hóa đơn điện tử (`DemoEInvoiceIntegration`)
- Cổng thanh toán trực tuyến (`DemoPaymentGateway`)
- Nhà cung cấp thông báo ngoài (`DemoNotificationProvider` chỉ lưu thông báo nội bộ In-App)
