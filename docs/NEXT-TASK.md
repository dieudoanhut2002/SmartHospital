# Công việc tiếp theo (NEXT-TASK)

Giai đoạn MVP theo `SMARTHOSPITAL_MASTER_SPEC.md` đã hoàn thành và vượt qua toàn bộ kiểm thử build/unit/integration/publish.

Các bước vận hành / mở rộng tiếp theo khi đưa vào triển khai thực tế:
1. Đánh giá chuyên môn y tế đối với danh mục dịch vụ, khoảng tham chiếu xét nghiệm, cảnh báo ngưỡng sinh hiệu và checklist phẫu thuật.
2. Thay thế các Adapter `Demo/Sandbox` (`ILabDeviceIntegration`, `IRadiologyIntegration`, `IPacsIntegration`, `IInsuranceIntegration`, `IEInvoiceIntegration`, `IPaymentGateway`, `INotificationProvider`) bằng tích hợp thật khi có hợp đồng kỹ thuật và tài khoản/thiết bị kiểm định.
3. Cấu hình HTTPS, chứng chỉ SSL, chiến lược backup SQL Server + `App_Data/Documents`, và thiết lập mật khẩu `BootstrapAdmin:Password` qua secret store/biến môi trường trên máy chủ đích.
