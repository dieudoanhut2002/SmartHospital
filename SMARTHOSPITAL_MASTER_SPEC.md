# SmartHospital — Product & Engineering Master Specification

**Document:** `SMARTHOSPITAL_MASTER_SPEC.md`  
**Version:** 1.0.0  
**Status:** Baseline for autonomous implementation  
**Language:** Vietnamese UI and user-facing documentation; English code identifiers  
**Primary database:** Microsoft SQL Server  
**Default environment:** Windows developer workstation using SQL Server LocalDB  
**Implementation principle:** Build the complete runnable application end-to-end without stopping for routine clarification.

---

## 0. Mệnh lệnh dành cho Agent

Bạn là Product Architect, Solution Architect, Senior .NET Engineer, Database Engineer, UI/UX Engineer, QA Engineer và Technical Writer chịu trách nhiệm triển khai dự án **SmartHospital** từ đầu đến cuối.

Đọc toàn bộ tài liệu này trước khi tạo mã nguồn. Đây là đặc tả nền tảng và là nguồn quyết định chính cho phạm vi MVP, kiến trúc, nghiệp vụ, dữ liệu, giao diện, bảo mật, kiểm thử và triển khai.

### 0.1. Yêu cầu thực thi

1. Tự chủ triển khai toàn bộ dự án theo tài liệu này. Không dừng lại để hỏi người dùng những câu hỏi thông thường, không yêu cầu người dùng lựa chọn giữa các phương án kỹ thuật nhỏ.
2. Khi thiếu chi tiết, áp dụng các nguyên tắc, mặc định và quy tắc nghiệp vụ trong tài liệu; ghi giả định vào `docs/ASSUMPTIONS.md`, tiếp tục thực hiện.
3. Chỉ được dừng để báo cáo nếu gặp trở ngại thực sự không thể tự xử lý, ví dụ thiếu quyền truy cập hệ thống bắt buộc, công cụ không thể cài đặt, hoặc yêu cầu có nguy cơ gây mất dữ liệu. Không được coi việc chưa rõ một chi tiết UI nhỏ là trở ngại.
4. Không chỉ tạo skeleton, mockup hoặc tài liệu. Phải tạo mã nguồn chạy được, database, migration, dữ liệu khởi tạo, giao diện, API nội bộ cần thiết, kiểm thử, hướng dẫn chạy và bản build có thể triển khai trên máy khác.
5. Triển khai theo từng vertical slice có thể chạy được. Sau mỗi giai đoạn phải build và test; sửa lỗi trước khi chuyển sang giai đoạn tiếp theo.
6. Không để lại nút bấm giả, trang rỗng, TODO quan trọng, dữ liệu hard-code giả làm dữ liệu thật hoặc chức năng chỉ hiển thị mà không lưu được.
7. Tích hợp bên ngoài chưa có tài khoản/thiết bị thật phải được thiết kế qua adapter/interface. Cung cấp chế độ demo/sandbox rõ ràng, không giả vờ đã kết nối hệ thống thật.
8. Không tuyên bố phần mềm đủ điều kiện vận hành lâm sàng hoặc tuân thủ pháp luật chỉ vì build/test thành công. Những phần cần chuyên gia y tế, kiểm định thiết bị, chứng nhận hoặc tích hợp cơ quan quản lý phải được đánh dấu rõ.
9. Duy trì các file trạng thái:
   - `AGENT.md`: chỉ dẫn ngắn cho Agent và cách làm việc.
   - `docs/PROJECT-STATUS.md`: trạng thái tổng thể, chức năng đã hoàn thành, còn thiếu.
   - `docs/DEVELOPMENT-LOG.md`: nhật ký thay đổi, lệnh build/test và kết quả.
   - `docs/NEXT-TASK.md`: công việc tiếp theo cụ thể.
   - `docs/ASSUMPTIONS.md`: các giả định đã áp dụng.
   - `docs/DECISIONS.md`: các quyết định kiến trúc/nghiệp vụ quan trọng.
10. Cập nhật trạng thái sau mỗi giai đoạn. Khi hoàn thành, cung cấp tổng kết, cách chạy, tài khoản demo, cách đổi connection string, cách backup/restore và các giới hạn chưa thể xác minh.

### 0.2. Definition of Done toàn dự án

Dự án chỉ được coi là hoàn thành khi:

- Solution mở và build thành công bằng SDK được chọn.
- Có thể chạy trên máy Windows mới đã cài runtime/prerequisites, không cần sửa source code.
- Nếu database chưa tồn tại, ứng dụng tự tạo database và schema từ migration.
- Nếu database đã tồn tại, ứng dụng không xóa hoặc ghi đè dữ liệu; chỉ áp dụng migration còn thiếu theo cấu hình.
- Có seed dữ liệu ban đầu có kiểm soát và có thể chạy lặp lại an toàn.
- Có đăng nhập, đăng xuất, phân quyền theo vai trò và phạm vi khoa/phòng.
- Các luồng nghiệp vụ MVP từ tiếp đón đến khám, chỉ định, kết quả, kê đơn/cấp phát và thanh toán có thể thực hiện xuyên suốt.
- Các thao tác quan trọng có audit log.
- Có validation phía client và server, xử lý lỗi, logging và thông báo rõ ràng bằng tiếng Việt.
- Có unit test và integration test cho các quy tắc nghiệp vụ quan trọng.
- Có README đầy đủ và hướng dẫn publish/deploy trên máy khác.
- Không còn lỗi build, lỗi test đã biết chưa được ghi nhận, hoặc chức năng cốt lõi chỉ là placeholder.

---

# 1. Tầm nhìn và phạm vi sản phẩm

## 1.1. Tên sản phẩm

**SmartHospital** — Hệ thống quản lý bệnh viện tích hợp.

## 1.2. Mục tiêu

Xây dựng nền tảng quản lý hoạt động của một bệnh viện đa khoa độc lập, có khả năng mở rộng quy mô, hỗ trợ nhiều khoa/phòng, nhiều điểm tiếp nhận, nhiều phòng khám, nhiều kho và nhiều nhóm người dùng.

Hệ thống phải quản lý xuyên suốt hành trình bệnh nhân:

1. Đăng ký/tiếp nhận.
2. Đặt lịch và xếp hàng.
3. Khám bệnh.
4. Chỉ định cận lâm sàng.
5. Thực hiện xét nghiệm/chẩn đoán hình ảnh.
6. Trả kết quả.
7. Kê đơn và cấp phát thuốc.
8. Nhập viện/điều trị nội trú.
9. Điều dưỡng và theo dõi.
10. Thanh toán, xuất viện và tái khám.

Định hướng kiến trúc phải cho phép mở rộng sang bệnh viện nhiều cơ sở, tích hợp thiết bị y tế và các hệ thống chuyên ngành, nhưng không làm phức tạp MVP không cần thiết.

## 1.3. Mô hình triển khai mặc định

- Một bệnh viện độc lập.
- Một tổ chức bệnh viện (`Hospital`) có thể có một hoặc nhiều cơ sở (`Facility`) trong mô hình dữ liệu, nhưng dữ liệu demo ban đầu chỉ tạo một cơ sở.
- Mỗi cơ sở có khoa, phòng, phòng khám, giường, quầy tiếp đón và kho.
- Một bệnh nhân có một mã hồ sơ thống nhất trong bệnh viện.
- Một bệnh nhân có thể có nhiều lượt khám, đợt điều trị, đơn thuốc và hóa đơn.
- Giai đoạn đầu triển khai dạng web responsive, không bắt buộc native mobile app.
- Giao diện tiếng Việt; code, database object, API contract và tên class dùng tiếng Anh.
- Múi giờ mặc định `Asia/Ho_Chi_Minh`; lưu timestamp hệ thống theo UTC, hiển thị theo múi giờ cấu hình.
- Đơn vị tiền tệ mặc định VND.

## 1.4. Phạm vi và giới hạn an toàn

Đây là phần mềm quản lý quy trình và thông tin y tế. Không tự động đưa ra chẩn đoán, phác đồ, liều thuốc hoặc quyết định điều trị. Không tạo thuật toán hỗ trợ quyết định lâm sàng nếu chưa có yêu cầu và kiểm định chuyên môn.

Các kết quả xét nghiệm, chỉ số tham chiếu, cảnh báo thuốc và quy tắc chuyên môn phải được cấu hình/duyệt bởi người có thẩm quyền. Dữ liệu demo phải được đánh dấu là dữ liệu giả lập.

Tích hợp BHYT, hóa đơn điện tử, chữ ký số, PACS/DICOM, LIS máy xét nghiệm và cổng thanh toán phải có adapter. Chỉ đánh dấu “đã tích hợp” khi đã kiểm thử với endpoint/thiết bị thật và thông tin xác thực hợp lệ.

---

# 2. Người dùng, phòng ban và phân quyền

## 2.1. Các phòng ban

| Phòng ban | Nhiệm vụ chính | Phân hệ sử dụng |
|---|---|---|
| Ban giám đốc | Điều hành, theo dõi chỉ số | Dashboard, Administration, BI |
| Phòng CNTT | Người dùng, cấu hình, vận hành | Administration, Audit, Integration |
| Tiếp đón | Định danh, đăng ký, điều phối | Patient, Reception, Appointment |
| Chăm sóc khách hàng | Hỗ trợ, lịch hẹn, phản hồi | Reception, Patient Portal |
| Khoa khám bệnh | Khám ngoại trú | Clinical EMR |
| Khoa lâm sàng | Điều trị, theo dõi | Clinical EMR, Inpatient, Nursing |
| Khoa cấp cứu | Tiếp nhận cấp cứu | Emergency, Clinical, Nursing |
| Khoa điều dưỡng | Chăm sóc và thực hiện y lệnh | Nursing |
| Khoa xét nghiệm | Mẫu bệnh phẩm, kết quả | LIS |
| Khoa chẩn đoán hình ảnh | Chụp, đọc kết quả | RIS/PACS adapter |
| Khoa dược | Duyệt và cấp phát thuốc | Pharmacy |
| Nhà thuốc | Bán/cấp thuốc ngoại trú | Pharmacy, Billing |
| Phòng mổ | Lịch và hồ sơ phẫu thuật | Surgery |
| Thu ngân | Thu, hoàn, đối soát | Billing |
| Tài chính kế toán | Giá, doanh thu, công nợ | Billing, Reports |
| Bảo hiểm y tế | Hồ sơ bảo hiểm, đối soát | Insurance adapter |
| Kho vật tư | Nhập xuất vật tư | Inventory |
| Nhân sự | Hồ sơ nhân viên, ca trực | HR |
| Kế hoạch tổng hợp | Báo cáo hoạt động | Reports, BI |
| Quản lý chất lượng | Theo dõi chất lượng | Quality/Reports |

## 2.2. Vai trò mặc định

Tạo các role sau và cho phép quản trị viên tùy chỉnh:

- `SystemAdmin`: quản trị kỹ thuật toàn hệ thống.
- `HospitalAdmin`: quản trị bệnh viện.
- `HospitalDirector`: ban giám đốc, xem dashboard và báo cáo theo quyền.
- `DepartmentHead`: trưởng khoa/phòng.
- `Receptionist`: tiếp đón.
- `CustomerService`: chăm sóc khách hàng.
- `Doctor`: bác sĩ.
- `Nurse`: điều dưỡng.
- `NurseManager`: điều dưỡng trưởng.
- `LabTechnician`: kỹ thuật viên xét nghiệm.
- `LabReviewer`: người duyệt kết quả xét nghiệm.
- `RadiologyTechnician`: kỹ thuật viên chẩn đoán hình ảnh.
- `Radiologist`: bác sĩ đọc kết quả hình ảnh.
- `Pharmacist`: dược sĩ.
- `PharmacyManager`: quản lý dược.
- `Cashier`: thu ngân.
- `Accountant`: kế toán.
- `InventoryStaff`: nhân viên kho.
- `HRStaff`: nhân viên nhân sự.
- `Auditor`: kiểm tra/audit chỉ đọc.
- `Patient`: bệnh nhân sử dụng cổng bệnh nhân.

Không chỉ phân quyền theo role. Phải kết hợp permission và data scope:
- Toàn bệnh viện.
- Cơ sở.
- Khoa/phòng.
- Phòng khám.
- Kho.
- Bệnh nhân/đợt khám được phân công.

Ví dụ: bác sĩ chỉ được truy cập hồ sơ bệnh nhân trong phạm vi được phép và có quan hệ công việc hợp lệ; quyền truy cập khẩn cấp nếu được triển khai phải có lý do, audit và chính sách riêng.

## 2.3. Ma trận quyền

Xây dựng RBAC có thể cấu hình:
- `Users`
- `Roles`
- `Permissions`
- `UserRoles`
- `RolePermissions`
- `UserDepartmentScopes`
- `UserFacilityScopes`

Permission theo dạng `module.resource.action`, ví dụ:
- `patient.read`
- `patient.create`
- `patient.update`
- `encounter.start`
- `clinical.note.sign`
- `lab.result.verify`
- `pharmacy.dispense`
- `billing.payment.collect`
- `billing.refund.approve`
- `admin.user.manage`

Các hành động nhạy cảm phải có quyền riêng: ký bệnh án, duyệt kết quả, hoàn tiền, sửa dữ liệu đã xác nhận, hủy phiếu, điều chỉnh tồn kho.

---

# 3. Danh sách ứng dụng và module

Các “App” dưới đây là các khu vực giao diện theo công việc. Không bắt buộc mỗi App là một backend/service riêng. Giai đoạn đầu dùng một ASP.NET Core web application với các Area/feature/module tách biệt, chia sẻ authentication và design system.

## 3.1. Hospital Administration

Đối tượng: quản trị viên, ban giám đốc, trưởng phòng.

Chức năng:
- Thông tin bệnh viện/cơ sở.
- Cấu trúc khoa, phòng, chuyên khoa.
- Phòng khám, giường bệnh, quầy tiếp đón.
- Danh mục dịch vụ, bảng giá.
- Danh mục ICD/bệnh, thuốc, đơn vị, đường dùng.
- Quản lý tài khoản, role, permission, phạm vi dữ liệu.
- Cấu hình ca làm việc, lịch hoạt động.
- Cấu hình số thứ tự, quy tắc mã hồ sơ.
- Cấu hình thông báo, mẫu in.
- Nhật ký hệ thống.
- Cấu hình tích hợp.

## 3.2. Patient Management

- Tạo hồ sơ bệnh nhân.
- Tìm kiếm theo mã bệnh nhân, họ tên, ngày sinh, số điện thoại, giấy tờ định danh.
- Kiểm tra trùng lặp và cảnh báo hồ sơ tương tự.
- Cập nhật thông tin hành chính.
- Thông tin liên hệ khẩn cấp/người đại diện.
- Lịch sử khám và điều trị.
- Dị ứng và cảnh báo quan trọng.
- Gắn cờ hồ sơ cần xác minh.
- Hợp nhất hồ sơ trùng lặp chỉ dành cho người có quyền, có audit và không xóa lịch sử.

Không dùng số điện thoại làm khóa định danh duy nhất. Không lưu dữ liệu nhạy cảm không cần thiết.

## 3.3. Reception & Appointment

- Tiếp nhận bệnh nhân đến khám.
- Đăng ký khám trực tiếp hoặc từ lịch hẹn.
- Tạo lượt khám (`Encounter`).
- Chọn cơ sở, chuyên khoa, phòng khám, bác sĩ.
- Cấp số thứ tự.
- Hàng đợi theo phòng khám/bác sĩ.
- Check-in, gọi số, bỏ lượt, chuyển hàng đợi.
- Trạng thái: `Booked`, `CheckedIn`, `Waiting`, `InProgress`, `Completed`, `Cancelled`, `NoShow`.
- In phiếu tiếp nhận và phiếu thứ tự.
- Quản lý lịch hẹn, nhắc lịch (qua notification adapter).
- Đăng ký nhập viện theo chỉ định.
- Chuyển bệnh nhân đến khu vực phù hợp.

## 3.4. Clinical EMR — Khám bệnh và hồ sơ bệnh án

- Danh sách bệnh nhân chờ khám.
- Mở hồ sơ và lượt khám.
- Ghi nhận lý do đến khám, triệu chứng, tiền sử.
- Ghi nhận dị ứng, thuốc đang sử dụng.
- Sinh hiệu: huyết áp, mạch, nhiệt độ, nhịp thở, SpO2, chiều cao, cân nặng.
- Khám lâm sàng.
- Chẩn đoán sơ bộ và chẩn đoán xác định.
- Chỉ định dịch vụ, xét nghiệm, chẩn đoán hình ảnh.
- Kê đơn thuốc.
- Ghi chú diễn biến.
- Kết luận, hướng điều trị, dặn dò và lịch tái khám.
- Lưu nháp, ký/xác nhận, khóa phiên bản.
- In phiếu khám, đơn thuốc, chỉ định.
- Xem kết quả từ LIS/RIS.
- Chuyển khoa hoặc chỉ định nhập viện.

Quy tắc:
- Hồ sơ chưa ký có thể chỉnh sửa theo quyền.
- Hồ sơ đã ký không được âm thầm sửa hoặc xóa. Sửa đổi phải tạo bản bổ sung/đính chính, giữ phiên bản cũ, người thực hiện, thời gian và lý do.
- Phải lưu tác giả và thời điểm cho ghi chú lâm sàng.
- Không tự sinh chẩn đoán hoặc phác đồ.

## 3.5. Nursing

- Danh sách bệnh nhân theo khoa/phòng/giường.
- Phân công điều dưỡng.
- Theo dõi sinh hiệu theo thời điểm.
- Phiếu chăm sóc.
- Theo dõi thực hiện y lệnh.
- Ghi nhận thuốc đã dùng/bỏ liều và lý do.
- Theo dõi dịch truyền theo dữ liệu do nhân viên nhập.
- Bàn giao ca trực.
- Cảnh báo giá trị ngoài ngưỡng cấu hình đã được phê duyệt.
- Nhật ký chăm sóc không được xóa âm thầm.

## 3.6. LIS — Laboratory

- Danh mục xét nghiệm và nhóm xét nghiệm.
- Nhận chỉ định từ Clinical.
- Tạo yêu cầu xét nghiệm (`LabOrder`).
- Lấy mẫu, nhận mẫu, từ chối mẫu và ghi lý do.
- Sinh mã mẫu/barcode.
- Quản lý loại mẫu và vị trí mẫu.
- Phân công kỹ thuật viên.
- Nhập kết quả thủ công trong MVP.
- Quản lý đơn vị, khoảng tham chiếu theo tuổi/giới nếu được cấu hình và phê duyệt.
- Kiểm tra kết quả, đánh dấu bất thường theo ngưỡng cấu hình.
- Duyệt kết quả bởi người có quyền.
- Trả kết quả về Encounter/EMR.
- Lịch sử sửa kết quả có version và audit.
- Trạng thái: `Ordered`, `Collected`, `Received`, `InProgress`, `PendingReview`, `Verified`, `Rejected`, `Cancelled`.

MVP không điều khiển trực tiếp máy xét nghiệm. Thiết kế interface để tích hợp HL7/FHIR hoặc giao thức nhà sản xuất ở giai đoạn sau.

## 3.7. RIS / PACS Integration

MVP:
- Danh mục dịch vụ chẩn đoán hình ảnh.
- Tiếp nhận chỉ định.
- Lịch thực hiện.
- Phân công kỹ thuật viên.
- Ghi nhận trạng thái.
- Nhập mô tả/kết luận.
- Đính kèm tài liệu hoặc đường dẫn ảnh demo có kiểm soát.
- Trả kết quả về EMR.

Mở rộng:
- DICOM modality worklist.
- PACS lưu trữ ảnh.
- DICOM viewer.
- HL7/FHIR integration.

Không tự xây dựng kho PACS quy mô lớn trong MVP. Tạo `IRadiologyIntegration`/`IPacsIntegration` và adapter giả lập được ghi nhãn rõ.

## 3.8. Pharmacy

- Danh mục thuốc, hoạt chất, hàm lượng, dạng bào chế.
- Đơn vị tính và quy đổi.
- Lô thuốc, hạn dùng.
- Kho dược và vị trí lưu.
- Nhập thuốc, xuất thuốc, điều chuyển.
- Nhận đơn thuốc/y lệnh.
- Kiểm tra tồn kho.
- Duyệt và cấp phát.
- Ghi nhận số lượng thực cấp, người cấp, thời gian.
- Thuốc ngoại trú và nội trú.
- Cảnh báo hết hạn/sắp hết hạn.
- Kiểm soát thuốc đặc biệt qua quyền và quy trình riêng.
- Lịch sử điều chỉnh tồn kho.

MVP chỉ cung cấp cảnh báo tương tác/chống chỉ định nếu có bộ dữ liệu đáng tin cậy được cấu hình. Không tự tạo khuyến nghị điều trị.

## 3.9. Inpatient & Bed Management

- Quản lý khoa nội trú, phòng, giường.
- Trạng thái giường: `Available`, `Occupied`, `Cleaning`, `Maintenance`, `Reserved`.
- Tiếp nhận bệnh nhân nhập viện.
- Phân giường.
- Chuyển giường/phòng/khoa.
- Theo dõi đợt điều trị (`Admission`).
- Liên kết Encounter, EMR, Nursing, Pharmacy, Billing.
- Ghi nhận ngày điều trị.
- Quy trình xuất viện.
- Tóm tắt ra viện và lịch tái khám.
- Theo dõi lịch sử sử dụng giường.

## 3.10. Emergency

MVP:
- Tiếp nhận cấp cứu nhanh.
- Ghi nhận thời điểm đến, lý do, tình trạng ban đầu.
- Phân loại ưu tiên do nhân viên y tế có thẩm quyền thực hiện.
- Ghi nhận khu vực/giường cấp cứu.
- Theo dõi trạng thái và chuyển khoa.
- Liên kết EMR, Nursing, LIS/RIS và Billing.

Không tự động phân loại cấp cứu bằng AI.

## 3.11. Surgery

MVP ở mức quản lý kế hoạch:
- Tạo yêu cầu phẫu thuật.
- Lịch phòng mổ.
- Phân công ekip.
- Trạng thái chuẩn bị/thực hiện/hoàn tất/hủy.
- Checklist do bệnh viện cấu hình và chuyên gia duyệt.
- Ghi nhận biên bản và tài liệu.
- Ghi nhận vật tư sử dụng.
- Liên kết EMR, Nursing, Pharmacy, Inventory và Billing.

Không mô phỏng hướng dẫn phẫu thuật hoặc quyết định gây mê.

## 3.12. Billing & Insurance

- Bảng giá dịch vụ theo thời gian hiệu lực.
- Ghi nhận khoản thu phát sinh từ khám, xét nghiệm, chẩn đoán hình ảnh, thuốc, giường và vật tư.
- Tạo hóa đơn/phiếu thu nội bộ.
- Tạm ứng, thanh toán một phần/nhiều lần.
- Nhiều phương thức thanh toán: tiền mặt, chuyển khoản, thẻ (MVP chỉ ghi nhận; payment gateway qua adapter).
- Hoàn tiền/điều chỉnh có lý do và quyền phê duyệt.
- Công nợ.
- Đối soát thu ngân.
- Phân tách số tiền bệnh nhân tự trả và bên chi trả khác.
- Quản lý thông tin thẻ bảo hiểm ở mức dữ liệu cơ bản.
- Tạo hồ sơ dữ liệu chờ tích hợp BHYT.
- Xuất hóa đơn điện tử qua adapter.

Quy tắc:
- Không xóa giao dịch tài chính đã ghi sổ; dùng reversal/refund/correction.
- Mỗi khoản phí có nguồn phát sinh, người tạo, thời gian, đơn giá áp dụng và trạng thái.
- Không tự khẳng định quyền lợi BHYT hoặc số tiền được cơ quan bảo hiểm chấp nhận khi chưa tích hợp/đối soát thật.

## 3.13. Inventory & Supply Chain

- Danh mục vật tư y tế và hàng hóa.
- Kho tổng, kho khoa/phòng.
- Nhập, xuất, điều chuyển.
- Lô, hạn dùng nếu phù hợp.
- Kiểm kê.
- Điều chỉnh có lý do và phê duyệt.
- Định mức tồn tối thiểu/tối đa.
- Nhà cung cấp.
- Đề xuất mua hàng, đơn mua hàng ở mức MVP cơ bản.
- Theo dõi thiết bị y tế, tài sản và lịch bảo trì ở giai đoạn mở rộng.

## 3.14. HR & Shift Scheduling

MVP:
- Hồ sơ nhân viên.
- Liên kết nhân viên với User.
- Chức danh, khoa/phòng.
- Lịch làm việc/ca trực cơ bản.
- Phân công bác sĩ/điều dưỡng vào phòng khám/khoa.
- Nghỉ phép ở mức ghi nhận cơ bản.

Mở rộng:
- Chấm công, lương, đánh giá hiệu suất, hợp đồng và quản lý chứng chỉ hành nghề.

## 3.15. Patient Portal

MVP có thể triển khai sau các luồng lõi:
- Đăng ký/đăng nhập bệnh nhân.
- Xem/cập nhật một số thông tin liên hệ.
- Đặt lịch khám.
- Xem lịch sử lịch hẹn.
- Xem kết quả được bệnh viện cho phép công bố.
- Xem đơn thuốc và hướng dẫn.
- Xem công nợ/hóa đơn.
- Nhận thông báo.

Không cho bệnh nhân truy cập trực tiếp toàn bộ EMR hoặc ghi sửa nội dung chuyên môn.

## 3.16. Dashboard & Reports

Dashboard theo quyền:
- Tổng lượt khám hôm nay.
- Bệnh nhân đang chờ.
- Số bệnh nhân đang điều trị nội trú.
- Công suất giường.
- Số chỉ định xét nghiệm đang chờ.
- Kết quả cần duyệt.
- Đơn thuốc chờ cấp.
- Doanh thu theo ngày/khoa.
- Tình trạng tồn kho cảnh báo.

Báo cáo phải lọc theo khoảng thời gian, cơ sở, khoa và trạng thái phù hợp. Không cho người dùng xem dữ liệu ngoài scope.

---

# 4. Quy trình nghiệp vụ đầu-cuối

## 4.1. Khám ngoại trú

1. Tiếp đón tìm hoặc tạo Patient.
2. Kiểm tra thông tin trùng và xác nhận danh tính.
3. Tạo Appointment hoặc Walk-in Encounter.
4. Chọn cơ sở, chuyên khoa, phòng khám và bác sĩ.
5. Cấp Queue Ticket.
6. Thu phí khám nếu quy trình bệnh viện yêu cầu.
7. Bệnh nhân vào hàng đợi.
8. Bác sĩ mở Encounter và tạo Clinical Note.
9. Bác sĩ ghi nhận sinh hiệu/triệu chứng/khám.
10. Bác sĩ tạo Diagnosis, Service Orders, Lab Orders, Imaging Orders hoặc Prescription.
11. Các khoa thực hiện chỉ định và cập nhật kết quả.
12. Bác sĩ xem kết quả, cập nhật kết luận và ký/xác nhận.
13. Dược sĩ cấp phát thuốc nếu có.
14. Thu ngân hoàn tất khoản phí còn lại.
15. Encounter chuyển `Completed`; lưu lịch sử đầy đủ.

## 4.2. Xét nghiệm

1. Clinical tạo Lab Order.
2. LIS tiếp nhận.
3. Nhân viên lấy mẫu và ghi nhận mẫu.
4. LIS cập nhật trạng thái nhận mẫu.
5. Kỹ thuật viên nhập kết quả.
6. Người có quyền kiểm tra và xác nhận.
7. Kết quả được khóa phiên bản.
8. Clinical nhận thông báo và hiển thị kết quả.
9. Billing ghi nhận khoản phí tương ứng theo quy tắc cấu hình.

## 4.3. Cấp phát thuốc

1. Bác sĩ tạo Prescription.
2. Pharmacy tiếp nhận.
3. Dược sĩ kiểm tra nội dung và tồn kho.
4. Chọn kho/lô phù hợp.
5. Xác nhận số lượng cấp.
6. Tạo Stock Transaction xuất kho.
7. Cập nhật trạng thái đơn.
8. Ghi nhận khoản phí.
9. Trả thông tin cấp phát về EMR.

## 4.4. Nhập viện và điều trị nội trú

1. Bác sĩ tạo chỉ định nhập viện.
2. Bộ phận tiếp nhận nội trú xác nhận.
3. Chọn khoa/phòng/giường trống.
4. Tạo Admission.
5. Điều dưỡng tiếp nhận và ghi nhận tình trạng ban đầu.
6. Bác sĩ lập kế hoạch điều trị và y lệnh.
7. Điều dưỡng ghi nhận chăm sóc/thực hiện y lệnh.
8. LIS/RIS/Pharmacy thực hiện các yêu cầu liên quan.
9. Billing ghi nhận chi phí theo sự kiện phát sinh.
10. Bác sĩ lập tóm tắt ra viện.
11. Hoàn tất thanh toán và xuất viện.
12. Giường chuyển sang trạng thái cần vệ sinh, sau đó mới thành Available.

## 4.5. Thanh toán

1. Hệ thống tổng hợp các khoản phí hợp lệ.
2. Thu ngân kiểm tra bảng kê.
3. Người bệnh thanh toán toàn bộ hoặc một phần.
4. Tạo Payment và Payment Allocation.
5. Không cho tổng phân bổ vượt số tiền thanh toán hoặc số dư phải thu.
6. Hoàn tiền phải tham chiếu giao dịch gốc, có lý do và quyền phù hợp.
7. Lưu audit và nhật ký đối soát.

---

# 5. Kiến trúc kỹ thuật bắt buộc

## 5.1. Công nghệ mặc định

Sử dụng hệ sinh thái Microsoft/.NET:

- Backend: ASP.NET Core trên phiên bản .NET LTS còn được hỗ trợ tại thời điểm triển khai. Ưu tiên .NET 10 LTS nếu SDK và môi trường build hỗ trợ ổn định; nếu không, chọn bản LTS đang được hỗ trợ và ghi rõ lý do.
- UI: ASP.NET Core MVC/Razor Pages hoặc Razor Views với Bootstrap 5, JavaScript và jQuery khi phù hợp. Ưu tiên UI server-rendered dễ triển khai, không bắt buộc SPA.
- ORM: Entity Framework Core tương thích với phiên bản .NET đã chọn.
- Database: Microsoft SQL Server.
- Authentication: ASP.NET Core Identity hoặc giải pháp tương đương tích hợp đầy đủ password hashing, lockout, cookie/session security.
- Logging: `ILogger` với structured logging; có thể dùng Serilog.
- Validation: server-side bắt buộc; client-side hỗ trợ trải nghiệm.
- API: REST API có versioning cho tích hợp và các client cần thiết.
- Documentation: Swagger/OpenAPI cho API trong môi trường Development.
- Tests: xUnit, integration test với database SQL Server thật hoặc test database phù hợp.
- Dependency Injection: built-in DI.
- Background tasks: hosted services khi thực sự cần; không tạo distributed infrastructure không cần thiết trong MVP.

Không thêm thư viện chỉ vì tiện nếu chức năng có thể làm tốt bằng framework hiện có. Ghi rõ license và phiên bản package.

## 5.2. Kiến trúc solution

Tổ chức solution theo hướng modular monolith, có thể tham khảo:

```text
SmartHospital/
├── SmartHospital.sln
├── src/
│   ├── SmartHospital.Web/
│   │   ├── Areas/
│   │   ├── Controllers/
│   │   ├── Views/
│   │   ├── wwwroot/
│   │   ├── Middleware/
│   │   └── Program.cs
│   ├── SmartHospital.Application/
│   │   ├── Common/
│   │   ├── Patients/
│   │   ├── Reception/
│   │   ├── Clinical/
│   │   ├── Laboratory/
│   │   ├── Pharmacy/
│   │   ├── Inpatient/
│   │   ├── Billing/
│   │   └── ...
│   ├── SmartHospital.Domain/
│   │   ├── Common/
│   │   ├── Patients/
│   │   ├── Clinical/
│   │   ├── Laboratory/
│   │   ├── Pharmacy/
│   │   └── ...
│   ├── SmartHospital.Infrastructure/
│   │   ├── Persistence/
│   │   ├── Identity/
│   │   ├── Integrations/
│   │   ├── Files/
│   │   └── ...
│   └── SmartHospital.Contracts/
├── tests/
│   ├── SmartHospital.UnitTests/
│   └── SmartHospital.IntegrationTests/
├── docs/
├── scripts/
├── appsettings.json
├── appsettings.Development.json
├── README.md
└── AGENT.md
```

Có thể điều chỉnh cấu trúc nếu có lý do kỹ thuật tốt, nhưng phải giữ ranh giới rõ:
- Domain không phụ thuộc Infrastructure/Web.
- Application điều phối use case và định nghĩa abstraction.
- Infrastructure triển khai persistence, identity, file và external adapters.
- Web chỉ xử lý HTTP/UI, authorization boundary và gọi Application.
- Contracts chứa DTO/API contracts dùng chung khi cần.

Không tạo một project riêng cho từng module nếu điều đó chỉ làm tăng độ phức tạp. Tuy nhiên, module phải có folder, namespace, entity, service, validator và persistence configuration tách biệt.

## 5.3. Quy tắc thiết kế

- Ưu tiên SOLID, separation of concerns, dependency inversion.
- Dùng DTO/ViewModel; không bind trực tiếp EF entity từ request.
- Không trả entity nội bộ ra API.
- Dùng async/await và CancellationToken cho I/O.
- Dùng pagination, filtering, sorting cho danh sách.
- Tránh N+1 query; chỉ Include dữ liệu cần thiết.
- Dùng transaction cho nghiệp vụ nhiều bước cần tính nhất quán.
- Không giữ transaction database trong lúc gọi external API.
- Dùng optimistic concurrency cho dữ liệu có khả năng được nhiều người sửa.
- Dùng soft delete chỉ với dữ liệu danh mục phù hợp; không soft-delete để che giấu lịch sử y tế/tài chính.
- Lưu tiền bằng `decimal`, không dùng `float/double`.
- Dùng `DateTimeOffset`/UTC cho thời điểm; dùng `DateOnly` cho ngày nghiệp vụ nếu phù hợp.
- Tạo index theo các truy vấn thực tế.
- Có unique constraint cho mã bệnh nhân, mã phiếu, số chứng từ trong phạm vi phù hợp.
- Không tạo generic repository quá mức nếu EF Core DbContext đã đáp ứng tốt.

---

# 6. SQL Server, Connection String và tự khởi tạo Database

**Đây là yêu cầu bắt buộc, phải triển khai và kiểm thử.**

## 6.1. Mục tiêu

- Dùng SQL Server làm database chính.
- Connection string đọc từ `appsettings.json` theo chuẩn ASP.NET Core Configuration.
- Mặc định chạy local trên Windows bằng SQL Server LocalDB.
- Người dùng có thể thay connection string mà không sửa source code.
- Khi database chưa tồn tại, ứng dụng có thể tự tạo database và toàn bộ bảng/schema.
- Khi chạy trên máy khác hoặc bản publish/build trên máy khác, nếu cấu hình trỏ đến một SQL Server hợp lệ và database chưa tồn tại, ứng dụng cũng tự khởi tạo database/schema.
- Khi database đã tồn tại, ứng dụng chỉ áp dụng migration còn thiếu theo chính sách; tuyệt đối không tự xóa database/bảng hoặc reset dữ liệu.
- Seed dữ liệu ban đầu phải idempotent: chạy nhiều lần không tạo trùng.

## 6.2. Connection string mặc định

Trong `appsettings.json`, cấu hình mặc định:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=SmartHospitalDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Database": {
    "AutoMigrate": true,
    "SeedInitialData": true
  }
}
```

Lưu ý:
- Đây là cấu hình mặc định cho máy Windows có cài SQL Server LocalDB.
- LocalDB không tự có sẵn trên mọi máy Windows. Máy đích phải cài SQL Server LocalDB hoặc SQL Server/SQL Server Express phù hợp.
- Với SQL Server cài local dạng instance, có thể đổi thành `Server=localhost\\SQLEXPRESS;Database=SmartHospitalDb;Trusted_Connection=True;TrustServerCertificate=True`.
- Với SQL Server trên máy chủ, cấu hình `Server=HOST,PORT;Database=SmartHospitalDb;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False`.
- Không commit mật khẩu production vào repository. Dùng User Secrets, environment variables hoặc secret store.
- Không hard-code connection string trong source code.

Cấu hình qua environment variable phải hoạt động theo chuẩn .NET:

```text
ConnectionStrings__DefaultConnection
Database__AutoMigrate
Database__SeedInitialData
```

## 6.3. EF Core migrations

- Sử dụng EF Core SQL Server provider.
- Tạo migration ban đầu trong source control.
- Mọi thay đổi schema phải đi qua migration.
- Không dùng `EnsureCreated()` cho ứng dụng có migrations.
- Không tự tạo bảng bằng các câu SQL rời rạc ngoài migration, trừ các đối tượng đặc biệt có tài liệu và migration quản lý.
- Migration phải có tên mô tả, được commit cùng code.
- Khi đổi model, Agent phải tạo migration mới và kiểm tra migration từ database rỗng.
- Cung cấp lệnh cập nhật database bằng CLI/Package Manager Console trong README.

## 6.4. Tự tạo database khi khởi động

Tạo một thành phần riêng, ví dụ `DatabaseInitializer`/`DatabaseStartupHostedService`, chạy sau khi cấu hình DI và trước khi nhận request.

Logic bắt buộc:

1. Đọc `ConnectionStrings:DefaultConnection`.
2. Kiểm tra connection string có tồn tại và hợp lệ.
3. Mở kết nối SQL Server.
4. Nếu server/instance chưa chạy hoặc LocalDB chưa được cài, trả lỗi hướng dẫn rõ ràng; không âm thầm chuyển sang SQLite hoặc database khác.
5. Nếu `Database:AutoMigrate=true`, gọi EF Core `Database.MigrateAsync()` để tạo database nếu chưa tồn tại và áp dụng migrations còn thiếu.
6. Không gọi `EnsureDeleted()`, không drop database, không xóa dữ liệu.
7. Chạy seed dữ liệu sau khi migration thành công nếu `Database:SeedInitialData=true`.
8. Seed phải có kiểm tra tồn tại và unique key để an toàn khi chạy lại.
9. Ghi log rõ: server/database, migration bắt đầu/kết thúc, seed thành công/thất bại. Không ghi password hoặc secret vào log.
10. Nếu migration thất bại, ứng dụng phải dừng khởi động hoặc hiển thị trạng thái lỗi rõ ràng; không chạy trong trạng thái database nửa khởi tạo.
11. Có xử lý tránh nhiều instance đồng thời cùng thực hiện migration trong môi trường multi-instance. Có thể dùng deployment migration job hoặc SQL application lock. Trong MVP single instance, tối thiểu phải ghi rõ giới hạn và không tạo seed trùng.
12. Không chạy migration tự động ở production nếu `Database:AutoMigrate=false`; khi đó cung cấp quy trình chạy migration riêng. Mặc định local/development là true.

Ví dụ logic tham khảo (Agent phải triển khai hoàn chỉnh theo phiên bản EF Core đã chọn):

```csharp
public static async Task InitializeDatabaseAsync(
    IServiceProvider services,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    var autoMigrate = configuration.GetValue<bool>("Database:AutoMigrate");
    var seedEnabled = configuration.GetValue<bool>("Database:SeedInitialData");

    await using var scope = services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<HospitalDbContext>();

    if (autoMigrate)
    {
        await db.Database.MigrateAsync(cancellationToken);
    }

    if (seedEnabled)
    {
        var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync(cancellationToken);
    }
}
```

Đây chỉ là khung minh họa. Phải bổ sung kiểm tra kết nối, logging, xử lý lỗi, concurrency strategy và chính sách theo environment.

## 6.5. Seed data

Tạo dữ liệu khởi tạo tối thiểu:
- Một bệnh viện demo.
- Một cơ sở.
- Các khoa/phòng cơ bản.
- Một số phòng khám.
- Một số giường demo.
- Danh mục chuyên khoa.
- Một số dịch vụ khám và xét nghiệm demo.
- Một số danh mục thuốc demo được đánh dấu dữ liệu mẫu.
- Roles và permissions mặc định.
- Tài khoản quản trị ban đầu.

Tài khoản mặc định:
- Username: `admin`
- Mật khẩu khởi tạo lấy từ cấu hình `BootstrapAdmin:Password` hoặc sinh ngẫu nhiên và hiển thị một lần trong log/console an toàn ở Development.
- Không được để một mật khẩu mặc định cố định, phổ biến hoặc yếu trong production.
- Bắt buộc đổi mật khẩu ở lần đăng nhập đầu tiên.
- Nếu tài khoản admin đã tồn tại, không reset password của tài khoản đó khi ứng dụng khởi động lại.

Seed dữ liệu demo chỉ chạy khi cấu hình cho phép. Không seed bệnh nhân giả vào môi trường production.

## 6.6. Hướng dẫn đổi database trên máy khác

README phải có các hướng dẫn:

**Máy Windows dùng LocalDB:**
1. Cài SQL Server Express LocalDB.
2. Giữ connection string mặc định.
3. Chạy ứng dụng.

**Máy có SQL Server Express:**
1. Cài và khởi động SQL Server Express.
2. Đổi `Server` thành instance thực tế.
3. Đảm bảo Windows account hoặc SQL login có quyền tạo database.
4. Chạy ứng dụng.

**Máy dùng SQL Server server/network:**
1. Cấu hình `ConnectionStrings__DefaultConnection`.
2. Đảm bảo network/firewall cho phép kết nối.
3. Cấp quyền tạo database trong lần khởi tạo đầu, hoặc DBA tạo database trước và cấp quyền schema phù hợp.
4. Bật/tắt AutoMigrate theo chính sách triển khai.

**Lưu ý quan trọng về quyền:** Không có ứng dụng nào có thể tự tạo database trên một SQL Server mà tài khoản kết nối không có quyền `CREATE DATABASE`. Nếu môi trường production không cấp quyền đó, DBA phải tạo database trước và cấp quyền migration. Đây là yêu cầu hạ tầng, không phải lỗi của ứng dụng.

## 6.7. Kiểm thử database bootstrap

Phải có test/manual verification cho các tình huống:
- Database chưa tồn tại → tự tạo database và toàn bộ schema.
- Database đã tồn tại, chưa có migration mới → ứng dụng khởi động bình thường.
- Database đã tồn tại, có migration mới → áp dụng migration còn thiếu.
- Chạy ứng dụng hai lần → không tạo seed trùng.
- Tài khoản admin đã đổi mật khẩu → restart không reset mật khẩu.
- Connection string sai → báo lỗi dễ hiểu.
- SQL Server dừng → báo lỗi rõ, không tự chuyển DB.
- `AutoMigrate=false` → không tự thay đổi schema.
- Build/publish trên máy khác → đọc connection string từ cấu hình ngoài source và bootstrap thành công nếu SQL Server/prerequisite có sẵn.

---

# 7. Database domain model

Agent phải thiết kế ERD và migrations. Danh sách dưới đây là định hướng entity tối thiểu, có thể bổ sung khi cần nhưng không bỏ các domain cốt lõi.

## 7.1. Organization & Identity

- `Hospitals`
- `Facilities`
- `Departments`
- `Specialties`
- `Rooms`
- `Clinics`
- `Beds`
- `Users`
- `Roles`
- `Permissions`
- `UserRoles`
- `RolePermissions`
- `UserDepartmentScopes`
- `Employees`
- `DoctorProfiles`
- `NurseProfiles`

## 7.2. Patient

- `Patients`
- `PatientIdentifiers`
- `PatientContacts`
- `PatientEmergencyContacts`
- `PatientAllergies`
- `PatientAlerts`
- `PatientMergeHistories`

## 7.3. Appointment & Reception

- `Appointments`
- `Encounters`
- `QueueTickets`
- `EncounterStatusHistories`
- `AdmissionRequests`

## 7.4. Clinical

- `ClinicalNotes`
- `ClinicalNoteVersions`
- `VitalSigns`
- `Diagnoses`
- `EncounterDiagnoses`
- `ClinicalOrders`
- `ServiceOrders`
- `Prescriptions`
- `PrescriptionItems`
- `ClinicalAttachments`

## 7.5. Laboratory & Imaging

- `LabTests`
- `LabOrders`
- `LabOrderItems`
- `Specimens`
- `LabResults`
- `LabResultVersions`
- `ImagingServices`
- `ImagingOrders`
- `ImagingReports`

## 7.6. Inpatient & Nursing

- `Admissions`
- `BedAssignments`
- `BedStatusHistories`
- `NursingAssignments`
- `NursingNotes`
- `NursingObservations`
- `MedicationAdministrations`
- `ShiftHandoverNotes`

## 7.7. Pharmacy & Inventory

- `Medicines`
- `MedicineUnits`
- `Warehouses`
- `WarehouseLocations`
- `MedicineBatches`
- `StockTransactions`
- `StockTransactionItems`
- `PrescriptionDispensings`
- `PrescriptionDispensingItems`
- `InventoryItems`
- `InventoryBatches`
- `PurchaseRequests`
- `Suppliers`

## 7.8. Billing

- `ServicePriceLists`
- `ServicePrices`
- `Invoices`
- `InvoiceItems`
- `Payments`
- `PaymentAllocations`
- `Refunds`
- `BillingAdjustments`
- `InsuranceProfiles`
- `InsuranceClaims` (dữ liệu nội bộ/adapter staging, không giả lập kết quả cơ quan bảo hiểm)

## 7.9. Audit & Integration

- `AuditLogs`
- `Documents`
- `Notifications`
- `IntegrationMessages`
- `OutboxMessages` nếu sử dụng transactional outbox.
- `SystemSettings`

## 7.10. Quy tắc dữ liệu chung

Các bảng nghiệp vụ cần cân nhắc:
- Primary key dạng `uniqueidentifier` hoặc `bigint`; chọn nhất quán. Khuyến nghị GUID cho entity cần đồng bộ/tích hợp và bigint cho bảng log lớn nếu phù hợp.
- `CreatedAtUtc`, `CreatedBy`.
- `UpdatedAtUtc`, `UpdatedBy`.
- `RowVersion` kiểu SQL Server `rowversion` cho entity cần concurrency.
- `IsActive` cho danh mục có vòng đời.
- `FacilityId`/`DepartmentId` khi cần scope.
- Foreign key và delete behavior rõ ràng.
- Index cho mã tra cứu, khóa ngoại, trạng thái và thời gian.
- Không dùng cascade delete cho dữ liệu y tế/tài chính quan trọng.
- Mã nghiệp vụ có unique index trong phạm vi phù hợp.
- Lưu snapshot đơn giá/tên dịch vụ trên chứng từ để lịch sử không đổi khi danh mục thay đổi.
- Lưu lịch sử trạng thái cho quy trình quan trọng.

Không lưu toàn bộ file nhị phân lớn vào SQL Server nếu không cần. Lưu file ở storage phù hợp, database lưu metadata, checksum, content type, kích thước và storage key. MVP có thể dùng local file storage ngoài web root; cấu hình đường dẫn và quyền truy cập.

---

# 8. Quy tắc nghiệp vụ và tính toàn vẹn

1. Patient là hồ sơ định danh; Encounter là một lượt khám; Admission là một đợt nội trú. Không gộp ba khái niệm.
2. Mỗi Encounter phải thuộc một Patient và một Facility.
3. Appointment có thể không tạo Encounter nếu bị hủy/no-show.
4. Không cấp QueueTicket trùng số trong cùng phạm vi/ngày/quầy theo quy tắc cấu hình.
5. Một giường không thể có hai assignment đang hiệu lực cùng thời điểm.
6. Không cấp phát thuốc vượt số lượng tồn có thể xuất; mọi xuất kho phải tạo stock transaction.
7. Tồn kho không được sửa trực tiếp bằng cách update số lượng tổng; phải tính từ giao dịch hoặc dùng ledger/balance được cập nhật nhất quán.
8. Kết quả xét nghiệm đã verified không được ghi đè; sửa phải tạo version/đính chính.
9. Clinical note đã ký không được sửa âm thầm.
10. Payment không được vượt số dư phải thu nếu không có quy tắc trả trước được cấu hình.
11. Refund phải tham chiếu payment gốc và không vượt số tiền còn có thể hoàn.
12. Đơn giá được chụp lại tại thời điểm phát sinh.
13. Mọi chuyển trạng thái quan trọng phải kiểm tra trạng thái hiện tại và quyền người thực hiện.
14. Không cho người dùng tự gán role/quyền cao hơn quyền hiện có.
15. Không để ID đoán được là đủ điều kiện truy cập; mọi truy vấn phải kiểm tra authorization scope ở server.
16. Dữ liệu y tế và tài chính phải có audit phù hợp.
17. Không xóa cứng hồ sơ bệnh án, kết quả, thanh toán hoặc giao dịch kho qua giao diện thông thường.
18. Hành động sửa/hủy phải yêu cầu lý do đối với dữ liệu đã xác nhận.
19. Các thao tác liên module cần đảm bảo transaction hoặc cơ chế eventual consistency có kiểm soát, idempotency và retry.
20. Tất cả danh sách có pagination và giới hạn kích thước trang.

---

# 9. Giao diện và trải nghiệm người dùng

## 9.1. Ngôn ngữ và phong cách

- Toàn bộ giao diện người dùng bằng tiếng Việt.
- Tên module, menu, trạng thái, validation message và thông báo lỗi dễ hiểu.
- Thiết kế hiện đại, chuyên nghiệp, sạch, dễ thao tác trong môi trường bệnh viện.
- Responsive cho desktop, laptop và tablet; các màn hình thao tác nhanh phải dùng được trên tablet.
- Không dùng màu sắc quá rực rỡ; trạng thái cảnh báo phải có màu và nhãn nhất quán.
- Có loading, empty state, error state, confirmation dialog.
- Có breadcrumb, page title, filter, search, pagination.
- Có form validation hiển thị gần trường nhập.
- Bảng dữ liệu hỗ trợ sort/filter phù hợp.
- Có nút thao tác rõ ràng, tránh thao tác nguy hiểm chỉ bằng một click.

## 9.2. Layout

- Sidebar theo module và quyền.
- Topbar có tên bệnh viện/cơ sở, người dùng, thông báo, đổi mật khẩu/đăng xuất.
- Dashboard theo role.
- Có thể chuyển cơ sở/khoa trong phạm vi được cấp quyền.
- Menu ẩn module không được cấp quyền hoặc chưa bật.
- Không hiển thị link mà người dùng không được truy cập.

## 9.3. Các màn hình tối thiểu

- Login, logout, change password, forgot/reset password theo cấu hình.
- Dashboard.
- Patient list/create/detail/edit.
- Appointment list/create/detail.
- Reception/check-in/queue board.
- Encounter workspace.
- Clinical note editor.
- Diagnosis and service order screens.
- Lab worklist, specimen, result entry, result verification.
- Pharmacy worklist, dispensing, stock view.
- Inpatient admission, bed board, transfer, discharge.
- Billing invoice, payment, refund.
- Department/room/bed/service catalog.
- User/role/permission management.
- Audit log viewer.
- System settings.
- Error/Access Denied/Not Found pages.

## 9.4. In ấn

Cung cấp mẫu in HTML/CSS print-friendly:
- Phiếu tiếp nhận.
- Phiếu số thứ tự.
- Phiếu chỉ định.
- Phiếu kết quả xét nghiệm.
- Đơn thuốc.
- Bảng kê chi phí.
- Phiếu thu.
- Tóm tắt ra viện.

Mẫu in phải có tên bệnh viện, mã bệnh nhân, thông tin lượt khám, thời gian và người lập theo ngữ cảnh. Không tự giả lập chữ ký hoặc con dấu.

---

# 10. Bảo mật, quyền riêng tư và audit

Dữ liệu sức khỏe là dữ liệu nhạy cảm. Áp dụng privacy-by-design và least privilege.

Bắt buộc:
- Password hashing chuẩn qua ASP.NET Core Identity hoặc thư viện chuẩn; không tự viết thuật toán hash mật khẩu.
- Cookie `HttpOnly`, `Secure` khi HTTPS, `SameSite` phù hợp.
- CSRF protection cho form.
- Validate và encode input/output để giảm XSS/SQL injection.
- Dùng parameterized query/EF Core; không nối chuỗi SQL từ input.
- Rate limiting cho login và endpoint nhạy cảm.
- Lockout/brute-force protection.
- Authorization kiểm tra ở server cho mọi endpoint.
- Không log mật khẩu, token, connection string, dữ liệu bệnh án đầy đủ hoặc thông tin định danh không cần thiết.
- Secrets không lưu trong source control.
- HTTPS khi deploy.
- File upload giới hạn kích thước, kiểm tra content type/extension, đổi tên file, lưu ngoài web root và kiểm soát download.
- Audit cho xem/sửa/ký hồ sơ, kết quả, đơn thuốc, cấp phát, thanh toán, hoàn tiền, tồn kho, quyền người dùng.
- Audit log chỉ cho đọc với người có quyền; không cho sửa/xóa qua UI.
- Có chính sách backup/restore và kiểm tra khả năng phục hồi.
- Có timeout/session expiration phù hợp.
- Có cơ chế vô hiệu hóa tài khoản nhân viên nghỉ việc.

Audit log tối thiểu:
- Actor/UserId.
- Action.
- EntityType/EntityId.
- Timestamp UTC.
- Facility/Department context.
- CorrelationId.
- IP/UserAgent nếu phù hợp chính sách.
- Reason cho thao tác nhạy cảm.
- Thay đổi trước/sau ở dạng đã lọc dữ liệu nhạy cảm; không sao chép bí mật vào log.

---

# 11. Tích hợp và khả năng mở rộng

Tạo abstraction cho:
- `ILabDeviceIntegration`
- `IRadiologyIntegration`
- `IPacsIntegration`
- `IInsuranceIntegration`
- `IEInvoiceIntegration`
- `IPaymentGateway`
- `INotificationProvider`
- `IDocumentStorage`

Trong MVP:
- Dùng adapter nội bộ/demo có nhãn `Demo` hoặc `Sandbox`.
- Không hard-code endpoint, token hoặc credentials.
- Có timeout, retry có giới hạn, idempotency key và logging correlation.
- Không retry vô hạn các giao dịch tài chính.
- Lưu trạng thái request/response cần thiết nhưng che dữ liệu nhạy cảm.
- Có màn hình theo dõi integration messages khi phù hợp.

Ưu tiên chuẩn mở phù hợp:
- HL7/FHIR cho trao đổi thông tin y tế.
- DICOM cho hình ảnh y khoa.
- REST/JSON cho API nội bộ.
- OpenAPI cho mô tả API.
Không tự tuyên bố tuân thủ đầy đủ một chuẩn nếu mới chỉ triển khai một phần.

---

# 12. Logging, monitoring và vận hành

- Structured logs.
- Correlation ID cho request và các luồng xuyên module.
- Global exception handling.
- Health checks cho ứng dụng và SQL Server.
- Không trả stack trace ra production.
- Log lỗi có context nhưng không rò rỉ dữ liệu bệnh nhân.
- Có trang trạng thái hệ thống chỉ dành cho quản trị viên.
- Có hướng dẫn backup/restore SQL Server.
- Có hướng dẫn lưu trữ và backup file đính kèm.
- Có hướng dẫn cập nhật version và migration.
- Có thể publish self-contained hoặc framework-dependent; README ghi rõ runtime cần cài.
- Cấu hình log level theo environment.

---

# 13. Kiểm thử và tiêu chí nghiệm thu

## 13.1. Unit tests

Kiểm thử tối thiểu:
- Patient duplicate detection.
- Appointment/Encounter state transitions.
- Queue ticket uniqueness.
- Bed assignment conflict.
- Lab result verification and amendment.
- Prescription dispensing quantity.
- Stock transaction balance.
- Invoice total calculation.
- Payment allocation and overpayment prevention.
- Refund limit.
- Permission and department scope checks.
- Seed idempotency.

## 13.2. Integration tests

- EF Core mapping và migration.
- SQL Server persistence.
- Database bootstrap trên database rỗng.
- Authentication và authorization.
- Tạo patient → appointment → encounter.
- Encounter → lab order → result → EMR.
- Prescription → dispense → stock transaction.
- Admission → bed assignment → discharge.
- Invoice → payment → refund.
- Audit log được tạo đúng.

Integration test phải chạy trên SQL Server thật hoặc SQL Server container/test instance tương thích. Không dựa hoàn toàn vào EF InMemory vì nó không kiểm tra hành vi SQL Server, constraint và migration.

## 13.3. UI/E2E smoke tests

Ít nhất kiểm thử:
- Đăng nhập.
- Tạo bệnh nhân.
- Đăng ký khám.
- Bác sĩ hoàn tất lượt khám.
- Tạo và trả kết quả xét nghiệm demo.
- Kê và cấp phát thuốc demo.
- Thu phí.
- Kiểm tra dashboard.
- Người dùng không đủ quyền bị chặn.

## 13.4. Kiểm tra chất lượng trước khi hoàn tất

Chạy:
- `dotnet restore`
- `dotnet build`
- `dotnet test`
- Migration từ database rỗng.
- Chạy ứng dụng bằng cấu hình mặc định.
- Publish Release.
- Chạy bản publish trên môi trường sạch có SQL Server/LocalDB.
- Kiểm tra cấu hình connection string override bằng environment variable.

Ghi lệnh và kết quả thực tế vào `docs/DEVELOPMENT-LOG.md`. Không ghi “passed” nếu chưa chạy.

---

# 14. Lộ trình triển khai bắt buộc

Agent phải triển khai tuần tự, nhưng tự động tiếp tục sau khi mỗi phase đạt Definition of Done.

## Phase 0 — Repository & Engineering Foundation

- Kiểm tra workspace.
- Tạo solution/project.
- Thiết lập nullable reference types, analyzers, formatting.
- Thiết lập DI, logging, exception handling.
- Thiết lập appsettings/environment configuration.
- Thiết lập CI hoặc script build/test.
- Tạo docs trạng thái.

**Đầu ra:** solution build được.

## Phase 1 — SQL Server & Identity Foundation

- EF Core SQL Server.
- DbContext, entity configurations.
- Initial migration.
- Auto database creation/migration.
- Idempotent seed.
- Identity, login/logout, password change.
- Roles/permissions/scopes.
- Admin screens.

**Đầu ra:** máy mới có SQL Server LocalDB có thể chạy ứng dụng và tự khởi tạo DB.

## Phase 2 — Master Data & Patient

- Hospital/facility/department/clinic/room/bed.
- Specialties/services.
- Patient CRUD/search/detail.
- Duplicate warning.
- Audit.

**Đầu ra:** quản lý danh mục và bệnh nhân.

## Phase 3 — Reception & Appointment

- Appointment.
- Walk-in.
- Encounter.
- Queue ticket/queue board.
- Check-in.
- Print ticket.

**Đầu ra:** tiếp đón và tạo lượt khám.

## Phase 4 — Clinical EMR

- Clinical workspace.
- Vital signs.
- Clinical notes.
- Diagnoses.
- Service orders.
- Prescriptions.
- Sign/lock/amendment rules.

**Đầu ra:** hoàn thành quy trình khám ngoại trú cơ bản.

## Phase 5 — LIS & Imaging Basic

- Lab catalog/order/specimen/result.
- Verify result.
- Imaging order/report.
- Integration adapter demo.
- Trả kết quả về EMR.

**Đầu ra:** quy trình chỉ định và nhận kết quả.

## Phase 6 — Pharmacy & Inventory

- Medicine catalog/batches.
- Warehouse/stock ledger.
- Dispensing.
- Stock transaction.
- Low-stock/expiry warnings.

**Đầu ra:** kê đơn, cấp phát và kiểm soát tồn kho demo.

## Phase 7 — Billing

- Price list.
- Charge items.
- Invoice.
- Payment/allocation.
- Refund/correction.
- Cashier report.

**Đầu ra:** luồng khám có thể thu phí xuyên suốt.

## Phase 8 — Inpatient, Nursing & Emergency

- Admission.
- Bed board/assignment.
- Nursing notes/observations.
- Medication administration record.
- Emergency intake.
- Discharge.

**Đầu ra:** quản lý nội trú cơ bản.

## Phase 9 — Surgery, Patient Portal & Reports

- Surgery scheduling/workflow basic.
- Patient portal basic.
- Dashboard/reporting.
- Notifications adapter.
- Print templates hoàn chỉnh.

## Phase 10 — Hardening & Release

- Security review.
- Performance review.
- Full tests.
- Backup/restore documentation.
- Publish profile/scripts.
- Clean-machine deployment test.
- Final documentation and handover.

---

# 15. Quy tắc làm việc của Agent

1. Trước khi code, đọc tài liệu này và tạo kế hoạch task chi tiết trong `docs/IMPLEMENTATION-PLAN.md`.
2. Không hỏi người dùng xác nhận từng phase. Tự triển khai tuần tự.
3. Nếu phát hiện mâu thuẫn, ưu tiên theo thứ tự:
   - An toàn người bệnh, bảo mật và toàn vẹn dữ liệu.
   - Các yêu cầu bắt buộc trong tài liệu này.
   - Quy tắc nghiệp vụ đã nêu.
   - Quy ước kỹ thuật của framework.
   - Giả định đơn giản nhất có thể mở rộng.
4. Ghi lại quyết định khác với tài liệu trong `docs/DECISIONS.md` và lý do.
5. Không tự mở rộng scope sang AI chẩn đoán, telemedicine, bảo hiểm thật hoặc tích hợp thiết bị thật nếu chưa có cấu hình/hợp đồng kỹ thuật.
6. Không dùng dữ liệu bệnh nhân thật trong demo/test.
7. Mọi chức năng tạo/sửa/xóa phải có validation, authorization, audit phù hợp và test.
8. Không đánh dấu task hoàn thành khi chỉ mới tạo UI hoặc API nhưng chưa kết nối database.
9. Không bỏ qua lỗi build/test. Phải sửa hoặc ghi rõ blocker có thể tái hiện.
10. Nếu môi trường không có SQL Server LocalDB, không được giả vờ bootstrap thành công. Ghi rõ prerequisite và tiếp tục các phần không phụ thuộc database nếu hợp lý.
11. Cập nhật `PROJECT-STATUS.md`, `DEVELOPMENT-LOG.md`, `NEXT-TASK.md` sau mỗi phase.
12. Khi hoàn tất, README phải hướng dẫn người mới clone repository, cài prerequisite, cấu hình connection string, chạy ứng dụng, đăng nhập lần đầu, đổi mật khẩu, publish và backup/restore.

---

# 16. README bắt buộc phải có

README cuối cùng phải bao gồm:
- Giới thiệu SmartHospital.
- Danh sách tính năng đã triển khai và trạng thái.
- Kiến trúc và sơ đồ module.
- Yêu cầu môi trường.
- Cài đặt .NET SDK/runtime.
- Cài SQL Server LocalDB hoặc SQL Server Express.
- Cấu hình `appsettings.json`.
- Cấu hình connection string qua environment variable.
- Chạy migration thủ công khi cần.
- Cách chạy Development.
- Cách publish Release.
- Cách chạy bản publish trên máy khác.
- Cách tạo database lần đầu.
- Cách tạo/reset tài khoản admin an toàn.
- Backup/restore database.
- Backup file đính kèm.
- Chạy unit/integration tests.
- Các adapter demo và giới hạn tích hợp thật.
- Troubleshooting các lỗi SQL Server phổ biến.

---

# 17. Kết quả bàn giao cuối cùng

Agent phải bàn giao:

1. Source code hoàn chỉnh trong repository.
2. Solution/project build thành công.
3. EF Core migrations.
4. Database bootstrap và seed.
5. Các ứng dụng/phân hệ theo scope đã triển khai.
6. Unit tests và integration tests.
7. Tài liệu kiến trúc và ERD.
8. `README.md`.
9. `AGENT.md`.
10. `docs/PROJECT-STATUS.md`.
11. `docs/DEVELOPMENT-LOG.md`.
12. `docs/NEXT-TASK.md`.
13. `docs/ASSUMPTIONS.md`.
14. `docs/DECISIONS.md`.
15. `docs/IMPLEMENTATION-PLAN.md`.
16. Publish profile hoặc script build/publish.
17. Báo cáo các chức năng đã hoàn thành, chưa hoàn thành, các giới hạn và các tích hợp cần cấu hình thật.

**Yêu cầu cuối:** Hãy bắt đầu bằng việc kiểm tra workspace và tạo kế hoạch triển khai, sau đó thực hiện liên tục đến khi hoàn tất toàn bộ các phase. Không chỉ dừng ở việc phân tích hoặc đề xuất. Không hỏi lại những nội dung đã có mặc định trong tài liệu này.
