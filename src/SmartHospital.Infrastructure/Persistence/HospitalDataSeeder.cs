using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Billing;
using SmartHospital.Domain.Clinical;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Identity;
using SmartHospital.Domain.Laboratory;
using SmartHospital.Domain.Organization;
using SmartHospital.Domain.Patients;
using SmartHospital.Domain.Pharmacy;
using SmartHospital.Infrastructure.Identity;

namespace SmartHospital.Infrastructure.Persistence;

public class HospitalDataSeeder(
    HospitalDbContext db,
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<HospitalDataSeeder> logger) : IDataSeeder
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedPermissionsAsync(cancellationToken);
        await SeedRolesAndRolePermissionsAsync(cancellationToken);

        var (hospital, facility, departments) = await SeedOrganizationAsync(cancellationToken);
        await SeedCatalogsAsync(facility.Id, departments, cancellationToken);
        await SeedSystemSettingsAsync(cancellationToken);
        await SeedBootstrapAdminAsync(facility.Id, cancellationToken);

        var seedDemoData = configuration.GetValue("Database:SeedDemoData", environment.IsDevelopment());
        if (seedDemoData)
        {
            await SeedDemoUsersAndPatientsAsync(facility.Id, departments, cancellationToken);
        }
    }

    private async Task SeedPermissionsAsync(CancellationToken cancellationToken)
    {
        var existingCodes = await db.Permissions
            .Select(p => p.Code)
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

        var added = false;
        foreach (var (code, module, name) in PermissionCodes.All)
        {
            if (existingSet.Contains(code)) continue;
            db.Permissions.Add(new Permission
            {
                Code = code,
                Module = module,
                Name = name
            });
            added = true;
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task SeedRolesAndRolePermissionsAsync(CancellationToken cancellationToken)
    {
        foreach (var (code, displayName, description) in RoleNames.All)
        {
            var role = await roleManager.FindByNameAsync(code);
            if (role is null)
            {
                role = new ApplicationRole
                {
                    Name = code,
                    DisplayName = displayName,
                    Description = description,
                    IsSystemRole = true
                };
                await roleManager.CreateAsync(role);
            }
        }

        var allPermissions = await db.Permissions.ToDictionaryAsync(p => p.Code, p => p.Id, cancellationToken);
        var allRoles = await roleManager.Roles.ToListAsync(cancellationToken);
        var existingRolePerms = await db.RolePermissions
            .Select(rp => new { rp.RoleId, rp.PermissionId })
            .ToListAsync(cancellationToken);
        var existingSet = existingRolePerms.Select(x => (x.RoleId, x.PermissionId)).ToHashSet();

        var roleMatrix = BuildRolePermissionMatrix();
        var added = false;

        foreach (var role in allRoles)
        {
            if (role.Name is null || !roleMatrix.TryGetValue(role.Name, out var permCodes)) continue;

            foreach (var permCode in permCodes)
            {
                if (!allPermissions.TryGetValue(permCode, out var permId)) continue;
                if (existingSet.Contains((role.Id, permId))) continue;

                db.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
                existingSet.Add((role.Id, permId));
                added = true;
            }
        }

        if (added)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<(Hospital Hospital, Facility Facility, Dictionary<string, Department> Departments)> SeedOrganizationAsync(CancellationToken cancellationToken)
    {
        var hospital = await db.Hospitals.FirstOrDefaultAsync(h => h.Code == "BV-DEMO", cancellationToken);
        if (hospital is null)
        {
            hospital = new Hospital
            {
                Code = "BV-DEMO",
                Name = "Bệnh viện Đa khoa SmartHospital [Demo]",
                TaxCode = "0100000001",
                Phone = "024-3999-0000",
                Email = "lienhe@smarthospital.demo",
                Address = "Số 1 Đại lộ Sức Khỏe, Quận Hoàn Kiếm, Hà Nội",
                TimeZoneId = "Asia/Ho_Chi_Minh",
                CurrencyCode = "VND"
            };
            db.Hospitals.Add(hospital);
            await db.SaveChangesAsync(cancellationToken);
        }

        var facility = await db.Facilities.FirstOrDefaultAsync(f => f.HospitalId == hospital.Id && f.Code == "CS01", cancellationToken);
        if (facility is null)
        {
            facility = new Facility
            {
                HospitalId = hospital.Id,
                Code = "CS01",
                Name = "Cơ sở Chính - Trung tâm",
                Phone = "024-3999-0001",
                Address = "Số 1 Đại lộ Sức Khỏe, Hà Nội",
                IsDefault = true
            };
            db.Facilities.Add(facility);
            await db.SaveChangesAsync(cancellationToken);
        }

        var deptDefs = new (string Code, string Name, DepartmentType Type, string Location)[]
        {
            ("RECEP", "Khoa Tiếp đón & CSKH", DepartmentType.Reception, "Tầng 1 - Sảnh chính"),
            ("OPD", "Khoa Khám bệnh", DepartmentType.Outpatient, "Tầng 1-2 - Khu Khám"),
            ("INTMED", "Khoa Nội tổng hợp", DepartmentType.ClinicalInpatient, "Tầng 4 - Khu Nội"),
            ("SURG", "Khoa Ngoại & Phẫu thuật", DepartmentType.Surgery, "Tầng 5 - Khu Ngoại"),
            ("EMERG", "Khoa Cấp cứu", DepartmentType.Emergency, "Tầng 1 - Cổng Cấp cứu"),
            ("NURSE", "Phòng Điều dưỡng", DepartmentType.Nursing, "Tầng 3"),
            ("LAB", "Khoa Xét nghiệm", DepartmentType.Laboratory, "Tầng 2 - Khu CLS"),
            ("IMG", "Khoa Chẩn đoán hình ảnh", DepartmentType.Radiology, "Tầng 2 - Khu CLS"),
            ("PHARM", "Khoa Dược & Vật tư", DepartmentType.Pharmacy, "Tầng 1 - Khu Dược"),
            ("FIN", "Phòng Tài chính Kế toán", DepartmentType.Finance, "Tầng 1 - Quầy Thu ngân")
        };

        var existingDepts = await db.Departments
            .Where(d => d.FacilityId == facility.Id)
            .ToDictionaryAsync(d => d.Code, cancellationToken);

        foreach (var (code, name, type, loc) in deptDefs)
        {
            if (existingDepts.ContainsKey(code)) continue;
            var d = new Department
            {
                FacilityId = facility.Id,
                Code = code,
                Name = name,
                DepartmentType = type,
                Location = loc
            };
            db.Departments.Add(d);
            existingDepts[code] = d;
        }
        await db.SaveChangesAsync(cancellationToken);

        // Specialties
        var specDefs = new (string Code, string Name)[]
        {
            ("NOI", "Nội tổng quát"),
            ("NGOAI", "Ngoại tổng quát"),
            ("NHI", "Nhi khoa"),
            ("TMH", "Tai Mũi Họng"),
            ("TIM", "Tim mạch")
        };
        var existingSpecs = await db.Specialties.ToDictionaryAsync(s => s.Code, cancellationToken);
        foreach (var (code, name) in specDefs)
        {
            if (existingSpecs.ContainsKey(code)) continue;
            var s = new Specialty { Code = code, Name = name };
            db.Specialties.Add(s);
            existingSpecs[code] = s;
        }
        await db.SaveChangesAsync(cancellationToken);

        // Rooms & Clinics
        var opdId = existingDepts["OPD"].Id;
        var intMedId = existingDepts["INTMED"].Id;
        var surgId = existingDepts["SURG"].Id;
        var emergId = existingDepts["EMERG"].Id;

        var roomDefs = new (string Code, string Name, Guid DeptId, RoomType Type, string Floor)[]
        {
            ("P101", "Phòng khám Nội 1", opdId, RoomType.Examination, "Tầng 1"),
            ("P102", "Phòng khám Nội 2", opdId, RoomType.Examination, "Tầng 1"),
            ("P103", "Phòng khám Ngoại", opdId, RoomType.Examination, "Tầng 1"),
            ("P104", "Phòng khám TMH", opdId, RoomType.Examination, "Tầng 2"),
            ("P401", "Phòng bệnh Nội 401", intMedId, RoomType.InpatientWard, "Tầng 4"),
            ("P402", "Phòng bệnh Nội 402", intMedId, RoomType.InpatientWard, "Tầng 4"),
            ("PM01", "Phòng mổ số 1", surgId, RoomType.OperatingRoom, "Tầng 5"),
            ("CC01", "Phòng Cấp cứu 1", emergId, RoomType.EmergencyBay, "Tầng 1")
        };
        var existingRooms = await db.Rooms.Where(r => r.FacilityId == facility.Id).ToDictionaryAsync(r => r.Code, cancellationToken);
        foreach (var (code, name, deptId, type, floor) in roomDefs)
        {
            if (existingRooms.ContainsKey(code)) continue;
            var r = new Room
            {
                FacilityId = facility.Id,
                DepartmentId = deptId,
                Code = code,
                Name = name,
                RoomType = type,
                Floor = floor
            };
            db.Rooms.Add(r);
            existingRooms[code] = r;
        }
        await db.SaveChangesAsync(cancellationToken);

        var clinicDefs = new (string Code, string Name, string SpecCode, string RoomCode, string Prefix)[]
        {
            ("PK-NOI1", "Phòng khám Nội Tổng quát 1", "NOI", "P101", "N1"),
            ("PK-NOI2", "Phòng khám Tim mạch", "TIM", "P102", "TM"),
            ("PK-NGOAI", "Phòng khám Ngoại Tổng quát", "NGOAI", "P103", "NG"),
            ("PK-TMH", "Phòng khám Tai Mũi Họng", "TMH", "P104", "TH")
        };
        var existingClinics = await db.Clinics.Where(c => c.FacilityId == facility.Id).Select(c => c.Code).ToHashSetAsync(cancellationToken);
        foreach (var (code, name, specCode, roomCode, prefix) in clinicDefs)
        {
            if (existingClinics.Contains(code)) continue;
            db.Clinics.Add(new Clinic
            {
                FacilityId = facility.Id,
                DepartmentId = opdId,
                SpecialtyId = existingSpecs[specCode].Id,
                RoomId = existingRooms[roomCode].Id,
                Code = code,
                Name = name,
                QueuePrefix = prefix
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        // Beds
        var existingBeds = await db.Beds.Where(b => b.FacilityId == facility.Id).Select(b => b.Code).ToHashSetAsync(cancellationToken);
        for (int i = 1; i <= 4; i++)
        {
            var code = $"G401-{i:00}";
            if (!existingBeds.Contains(code))
            {
                db.Beds.Add(new Bed
                {
                    FacilityId = facility.Id,
                    DepartmentId = intMedId,
                    RoomId = existingRooms["P401"].Id,
                    Code = code,
                    Name = $"Giường {i} - P401",
                    Status = BedStatus.Available,
                    DailyRate = 450000m
                });
            }
        }
        for (int i = 1; i <= 4; i++)
        {
            var code = $"G402-{i:00}";
            if (!existingBeds.Contains(code))
            {
                db.Beds.Add(new Bed
                {
                    FacilityId = facility.Id,
                    DepartmentId = intMedId,
                    RoomId = existingRooms["P402"].Id,
                    Code = code,
                    Name = $"Giường {i} - P402",
                    Status = BedStatus.Available,
                    DailyRate = 450000m
                });
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        return (hospital, facility, existingDepts);
    }

    private async Task SeedCatalogsAsync(Guid facilityId, Dictionary<string, Department> departments, CancellationToken cancellationToken)
    {
        // Services
        var svcDefs = new (string Code, string Name, string Category, decimal Price)[]
        {
            ("KB-NOI", "Khám bệnh Nội khoa", "Khám bệnh", 150000m),
            ("KB-CHUYENKHOA", "Khám chuyên khoa", "Khám bệnh", 200000m),
            ("KB-CAPCUU", "Khám cấp cứu", "Khám bệnh", 250000m),
            ("TT-TIEM", "Thủ thuật tiêm/truyền dịch", "Thủ thuật", 80000m),
            ("TT-THAYBANG", "Thay băng vết thương", "Thủ thuật", 120000m)
        };
        var existingSvcs = await db.ServiceCatalogs.Select(s => s.Code).ToHashSetAsync(cancellationToken);
        foreach (var (code, name, cat, price) in svcDefs)
        {
            if (existingSvcs.Contains(code)) continue;
            db.ServiceCatalogs.Add(new ServiceCatalog
            {
                Code = code,
                Name = name,
                Category = cat,
                DepartmentId = departments["OPD"].Id,
                DefaultPrice = price
            });
        }

        // Price List
        if (!await db.ServicePriceLists.AnyAsync(p => p.Code == "BG-2026", cancellationToken))
        {
            db.ServicePriceLists.Add(new ServicePriceList
            {
                Code = "BG-2026",
                Name = "Bảng giá dịch vụ chuẩn 2026",
                EffectiveFrom = new DateOnly(2026, 1, 1),
                IsDefault = true
            });
        }

        // ICD-10 sample diagnoses
        var icdDefs = new (string Code, string Name, string Chapter)[]
        {
            ("J06.9", "Nhiễm trùng đường hô hấp trên cấp", "Hệ hô hấp"),
            ("I10", "Tăng huyết áp vô căn (nguyên phát)", "Hệ tuần hoàn"),
            ("E11", "Đái tháo đường týp 2", "Nội tiết"),
            ("K29.7", "Viêm dạ dày", "Hệ tiêu hóa"),
            ("R50.9", "Sốt, không rõ nguyên nhân", "Triệu chứng"),
            ("J18.9", "Viêm phổi, không đặc hiệu", "Hệ hô hấp")
        };
        var existingIcds = await db.Diagnoses.Select(d => d.IcdCode).ToHashSetAsync(cancellationToken);
        foreach (var (code, name, chap) in icdDefs)
        {
            if (existingIcds.Contains(code)) continue;
            db.Diagnoses.Add(new Diagnosis { IcdCode = code, Name = name, Chapter = chap });
        }

        // Lab tests
        var labDefs = new (string Code, string Name, string Cat, string Specimen, string Unit, string RefText, decimal? Min, decimal? Max, decimal Price)[]
        {
            ("XN-CTM", "Tổng phân tích tế bào máu (CBC)", "Huyết học", "Máu toàn phần EDTA", "G/L", "WBC: 4.0 - 10.0", 4.0m, 10.0m, 120000m),
            ("XN-GLU", "Định lượng Glucose máu đói", "Sinh hóa", "Huyết thanh", "mmol/L", "3.9 - 6.4", 3.9m, 6.4m, 60000m),
            ("XN-AST", "Đo hoạt độ AST (GOT)", "Sinh hóa", "Huyết thanh", "U/L", "< 37", 0m, 37m, 65000m),
            ("XN-ALT", "Đo hoạt độ ALT (GPT)", "Sinh hóa", "Huyết thanh", "U/L", "< 40", 0m, 40m, 65000m),
            ("XN-CRP", "Định lượng CRP", "Miễn dịch", "Huyết thanh", "mg/L", "< 5.0", 0m, 5.0m, 110000m),
            ("XN-TPTNT", "Tổng phân tích nước tiểu", "Nước tiểu", "Nước tiểu", "Chỉ số", "Âm tính / Bình thường", null, null, 70000m)
        };
        var existingLabs = await db.LabTests.Select(t => t.Code).ToHashSetAsync(cancellationToken);
        foreach (var (code, name, cat, spec, unit, refText, min, max, price) in labDefs)
        {
            if (existingLabs.Contains(code)) continue;
            db.LabTests.Add(new LabTest
            {
                Code = code,
                Name = name,
                Category = cat,
                SpecimenType = spec,
                Unit = unit,
                ReferenceRangeText = refText,
                ReferenceMin = min,
                ReferenceMax = max,
                DefaultPrice = price
            });
        }

        // Imaging services
        var imgDefs = new (string Code, string Name, string Modality, decimal Price)[]
        {
            ("CDHA-XQNGUC", "Chụp X-Quang ngực thẳng", "X-Quang", 150000m),
            ("CDHA-SAOBUNG", "Siêu âm ổ bụng tổng quát", "Siêu âm", 220000m),
            ("CDHA-DIENTIM", "Điện tâm đồ (ECG)", "Thăm dò chức năng", 100000m),
            ("CDHA-CTSO", "Chụp CT sọ não không tiêm thuốc", "CT", 950000m)
        };
        var existingImgs = await db.ImagingServices.Select(i => i.Code).ToHashSetAsync(cancellationToken);
        foreach (var (code, name, mod, price) in imgDefs)
        {
            if (existingImgs.Contains(code)) continue;
            db.ImagingServices.Add(new ImagingService
            {
                Code = code,
                Name = name,
                Modality = mod,
                DefaultPrice = price
            });
        }

        // Warehouses
        var pharmDeptId = departments["PHARM"].Id;
        var pharmWh = await db.Warehouses.FirstOrDefaultAsync(w => w.FacilityId == facilityId && w.Code == "KHO-DUOC", cancellationToken);
        if (pharmWh is null)
        {
            pharmWh = new Warehouse
            {
                FacilityId = facilityId,
                DepartmentId = pharmDeptId,
                Code = "KHO-DUOC",
                Name = "Kho Dược Ngoại/Nội trú",
                WarehouseType = "Pharmacy"
            };
            db.Warehouses.Add(pharmWh);
        }
        if (!await db.Warehouses.AnyAsync(w => w.FacilityId == facilityId && w.Code == "KHO-VT", cancellationToken))
        {
            db.Warehouses.Add(new Warehouse
            {
                FacilityId = facilityId,
                DepartmentId = pharmDeptId,
                Code = "KHO-VT",
                Name = "Kho Vật tư Y tế",
                WarehouseType = "MainSupply"
            });
        }
        await db.SaveChangesAsync(cancellationToken);

        // Demo Medicines & Batches (explicitly marked IsDemoData = true)
        var medDefs = new (string Code, string Name, string Active, string Strength, string Form, string Unit, decimal Price)[]
        {
            ("THUOC-PARA500", "[Mẫu] Paracetamol 500mg", "Paracetamol", "500mg", "Viên nén", "Viên", 1500m),
            ("THUOC-AMOX500", "[Mẫu] Amoxicillin 500mg", "Amoxicillin", "500mg", "Viên nang", "Viên", 3500m),
            ("THUOC-OMEP20", "[Mẫu] Omeprazol 20mg", "Omeprazol", "20mg", "Viên nang", "Viên", 4000m),
            ("THUOC-AMLO5", "[Mẫu] Amlodipin 5mg", "Amlodipin", "5mg", "Viên nén", "Viên", 3000m),
            ("THUOC-NACL09", "[Mẫu] Natri Clorid 0.9% 500ml", "NaCl 0.9%", "500ml", "Dung dịch truyền", "Chai", 18000m)
        };
        var existingMeds = await db.Medicines.ToDictionaryAsync(m => m.Code, cancellationToken);
        foreach (var (code, name, active, str, form, unit, price) in medDefs)
        {
            if (!existingMeds.TryGetValue(code, out var med))
            {
                med = new Medicine
                {
                    Code = code,
                    Name = name,
                    ActiveIngredient = active,
                    Strength = str,
                    DosageForm = form,
                    BaseUnit = unit,
                    DefaultUnitPrice = price,
                    MinStockLevel = 20,
                    IsDemoData = true
                };
                db.Medicines.Add(med);
                existingMeds[code] = med;
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        foreach (var med in existingMeds.Values)
        {
            var batchNo = $"LOT-{med.Code}-2026";
            if (!await db.MedicineBatches.AnyAsync(b => b.WarehouseId == pharmWh.Id && b.MedicineId == med.Id && b.BatchNumber == batchNo, cancellationToken))
            {
                db.MedicineBatches.Add(new MedicineBatch
                {
                    WarehouseId = pharmWh.Id,
                    MedicineId = med.Id,
                    BatchNumber = batchNo,
                    ExpiryDate = HospitalTime.TodayVietnam().AddMonths(18),
                    QuantityOnHand = 500m,
                    UnitCost = Math.Round(med.DefaultUnitPrice * 0.7m, 0)
                });
            }
        }

        // Supplier & Inventory items
        if (!await db.Suppliers.AnyAsync(s => s.Code == "NCC-DEMO", cancellationToken))
        {
            db.Suppliers.Add(new Supplier
            {
                Code = "NCC-DEMO",
                Name = "Công ty Dược & Vật tư Y tế Demo",
                Phone = "024-3888-9999",
                Address = "Hà Nội"
            });
        }
        if (!await db.InventoryItems.AnyAsync(i => i.Code == "VT-GANGTAY", cancellationToken))
        {
            db.InventoryItems.Add(new InventoryItem
            {
                Code = "VT-GANGTAY",
                Name = "[Mẫu] Găng tay y tế không bột",
                Category = "Vật tư tiêu hao",
                Unit = "Đôi",
                DefaultUnitPrice = 3000m,
                MinStockLevel = 50,
                MaxStockLevel = 2000
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSystemSettingsAsync(CancellationToken cancellationToken)
    {
        var defaults = new (string Key, string Value, string Category, string Desc)[]
        {
            ("Hospital.TimeZone", "Asia/Ho_Chi_Minh", "General", "Múi giờ hiển thị mặc định"),
            ("Hospital.Currency", "VND", "Billing", "Đơn vị tiền tệ mặc định"),
            ("Reception.RequirePreConsultationFee", "false", "Reception", "Yêu cầu thu phí khám trước khi vào hàng đợi"),
            ("Integration.Mode", "Demo/Sandbox", "Integration", "Chế độ tích hợp ngoài hiện tại"),
            ("Portal.AllowSelfBooking", "true", "Portal", "Cho phép bệnh nhân đặt lịch qua Cổng bệnh nhân")
        };

        var existing = await db.SystemSettings.Select(s => s.Key).ToHashSetAsync(cancellationToken);
        var added = false;
        foreach (var (key, val, cat, desc) in defaults)
        {
            if (existing.Contains(key)) continue;
            db.SystemSettings.Add(new SystemSetting
            {
                Key = key,
                Value = val,
                Category = cat,
                Description = desc
            });
            added = true;
        }
        if (added) await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedBootstrapAdminAsync(Guid defaultFacilityId, CancellationToken cancellationToken)
    {
        var adminUser = await userManager.FindByNameAsync("admin");
        if (adminUser is not null)
        {
            // Never reset password of an existing admin user
            if (!await userManager.IsInRoleAsync(adminUser, RoleNames.SystemAdmin))
            {
                await userManager.AddToRoleAsync(adminUser, RoleNames.SystemAdmin);
            }
            return;
        }

        var configuredPassword = configuration["BootstrapAdmin:Password"];
        var generatedRandom = false;
        if (string.IsNullOrWhiteSpace(configuredPassword))
        {
            if (environment.IsDevelopment())
            {
                configuredPassword = GenerateSecurePassword();
                generatedRandom = true;
            }
            else
            {
                configuredPassword = GenerateSecurePassword();
                generatedRandom = true;
            }
        }

        adminUser = new ApplicationUser
        {
            UserName = "admin",
            Email = "admin@smarthospital.local",
            EmailConfirmed = true,
            FullName = "Quản trị viên Hệ thống",
            MustChangePassword = true,
            IsActive = true,
            DefaultFacilityId = defaultFacilityId,
            EmployeeCode = "EMP-ADMIN"
        };

        var createResult = await userManager.CreateAsync(adminUser, configuredPassword);
        if (!createResult.Succeeded)
        {
            var errors = string.Join("; ", createResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Không thể tạo tài khoản bootstrap admin: {errors}");
        }

        await userManager.AddToRoleAsync(adminUser, RoleNames.SystemAdmin);

        if (!await db.UserFacilityScopes.AnyAsync(s => s.UserId == adminUser.Id && s.FacilityId == defaultFacilityId, cancellationToken))
        {
            db.UserFacilityScopes.Add(new UserFacilityScope
            {
                UserId = adminUser.Id,
                FacilityId = defaultFacilityId,
                IsDefault = true
            });
            await db.SaveChangesAsync(cancellationToken);
        }

        if (generatedRandom)
        {
            logger.LogWarning(
                "========================================================================\n" +
                "TÀI KHOẢN QUẢN TRỊ KHỞI TẠO LẦN ĐẦU (BOOTSTRAP ADMIN):\n" +
                "  Username : admin\n" +
                "  Password : {GeneratedPassword}\n" +
                "Vui lòng đăng nhập và đổi mật khẩu ngay (bắt buộc ở lần đăng nhập đầu).\n" +
                "========================================================================",
                configuredPassword);
        }
        else
        {
            logger.LogInformation("Đã tạo tài khoản bootstrap 'admin' từ cấu hình BootstrapAdmin:Password (bắt buộc đổi mật khẩu khi đăng nhập lần đầu).");
        }
    }

    private async Task SeedDemoUsersAndPatientsAsync(
        Guid facilityId,
        Dictionary<string, Department> departments,
        CancellationToken cancellationToken)
    {
        var demoPassword = configuration["BootstrapAdmin:DemoUserPassword"] ?? "Demo@123456!";
        var demoUsers = new (string Username, string FullName, string Role, string DeptCode, string EmpCode)[]
        {
            ("reception.demo", "Nguyễn Thị Tiếp Đón [Demo]", RoleNames.Receptionist, "RECEP", "EMP-REC01"),
            ("doctor.demo", "BS. Trần Văn Khám [Demo]", RoleNames.Doctor, "OPD", "EMP-DOC01"),
            ("nurse.demo", "ĐD. Lê Thị Chăm Sóc [Demo]", RoleNames.Nurse, "INTMED", "EMP-NUR01"),
            ("lab.demo", "KTV. Phạm Xét Nghiệm [Demo]", RoleNames.LabTechnician, "LAB", "EMP-LAB01"),
            ("labreviewer.demo", "BS. Hoàng Duyệt XN [Demo]", RoleNames.LabReviewer, "LAB", "EMP-LAB02"),
            ("radiology.demo", "BS. Vũ Chẩn Đoán HA [Demo]", RoleNames.Radiologist, "IMG", "EMP-RAD01"),
            ("pharmacist.demo", "DS. Đỗ Dược Sĩ [Demo]", RoleNames.Pharmacist, "PHARM", "EMP-PHM01"),
            ("cashier.demo", "Bùi Thị Thu Ngân [Demo]", RoleNames.Cashier, "FIN", "EMP-CSH01"),
            ("patient.demo", "Bệnh nhân Demo [Demo]", RoleNames.Patient, "OPD", "PAT-USER01")
        };

        foreach (var (username, fullName, role, deptCode, empCode) in demoUsers)
        {
            var user = await userManager.FindByNameAsync(username);
            if (user is null)
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = $"{username}@smarthospital.demo",
                    EmailConfirmed = true,
                    FullName = fullName,
                    MustChangePassword = false,
                    IsActive = true,
                    DefaultFacilityId = facilityId,
                    DefaultDepartmentId = departments[deptCode].Id,
                    EmployeeCode = empCode
                };
                var res = await userManager.CreateAsync(user, demoPassword);
                if (res.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                }
            }

            if (user.Id != Guid.Empty && role != RoleNames.Patient)
            {
                if (!await db.UserFacilityScopes.AnyAsync(s => s.UserId == user.Id && s.FacilityId == facilityId, cancellationToken))
                {
                    db.UserFacilityScopes.Add(new UserFacilityScope
                    {
                        UserId = user.Id,
                        FacilityId = facilityId,
                        IsDefault = true
                    });
                }

                var deptId = departments[deptCode].Id;
                if (!await db.UserDepartmentScopes.AnyAsync(s => s.UserId == user.Id && s.FacilityId == facilityId && s.DepartmentId == deptId, cancellationToken))
                {
                    db.UserDepartmentScopes.Add(new UserDepartmentScope
                    {
                        UserId = user.Id,
                        FacilityId = facilityId,
                        DepartmentId = deptId
                    });
                }

                if (!await db.Employees.AnyAsync(e => e.EmployeeCode == empCode, cancellationToken))
                {
                    var emp = new Employee
                    {
                        FacilityId = facilityId,
                        DepartmentId = deptId,
                        UserId = user.Id,
                        EmployeeCode = empCode,
                        FullName = fullName,
                        JobTitle = role,
                        Email = user.Email
                    };
                    db.Employees.Add(emp);
                    if (role == RoleNames.Doctor)
                    {
                        db.DoctorProfiles.Add(new DoctorProfile { EmployeeId = emp.Id, LicenseNumber = "CCHN-DEMO-001", Degree = "BS.CKI" });
                    }
                    else if (role == RoleNames.Nurse)
                    {
                        db.NurseProfiles.Add(new NurseProfile { EmployeeId = emp.Id, LicenseNumber = "CCHN-DD-001", Level = "CN Điều dưỡng" });
                    }
                }
            }
        }
        await db.SaveChangesAsync(cancellationToken);

        // Demo patients
        var patientUser = await userManager.FindByNameAsync("patient.demo");
        var demoPatients = new (string Code, string Name, DateOnly Dob, Gender Gender, string Phone, string NationalId, Guid? PortalUserId)[]
        {
            ("BN-DEMO-001", "Nguyễn Văn An [Mẫu]", new DateOnly(1988, 5, 14), Gender.Male, "0901000111", "001088000111", patientUser?.Id),
            ("BN-DEMO-002", "Trần Thị Bình [Mẫu]", new DateOnly(1995, 9, 20), Gender.Female, "0901000222", "001195000222", null),
            ("BN-DEMO-003", "Lê Hoàng Minh [Mẫu]", new DateOnly(1962, 12, 2), Gender.Male, "0901000333", "001062000333", null)
        };

        foreach (var (code, name, dob, gender, phone, nid, portalUid) in demoPatients)
        {
            if (await db.Patients.AnyAsync(p => p.PatientCode == code, cancellationToken)) continue;
            db.Patients.Add(new Patient
            {
                PatientCode = code,
                FullName = name,
                NormalizedName = name.ToUpperInvariant(),
                DateOfBirth = dob,
                Gender = gender,
                PhoneNumber = phone,
                NationalId = nid,
                Address = "Hà Nội (Dữ liệu mẫu)",
                PortalUserId = portalUid,
                IsDemoData = true
            });
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateSecurePassword()
    {
        const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string lower = "abcdefghijkmnopqrstuvwxyz";
        const string digits = "23456789";
        const string special = "@#$!*";
        const string all = upper + lower + digits + special;

        var chars = new char[16];
        chars[0] = upper[RandomNumberGenerator.GetInt32(upper.Length)];
        chars[1] = lower[RandomNumberGenerator.GetInt32(lower.Length)];
        chars[2] = digits[RandomNumberGenerator.GetInt32(digits.Length)];
        chars[3] = special[RandomNumberGenerator.GetInt32(special.Length)];
        for (int i = 4; i < chars.Length; i++)
        {
            chars[i] = all[RandomNumberGenerator.GetInt32(all.Length)];
        }
        RandomNumberGenerator.Shuffle(chars);
        return new string(chars);
    }

    private static Dictionary<string, string[]> BuildRolePermissionMatrix()
    {
        var allPerms = PermissionCodes.All.Select(p => p.Code).ToArray();
        return new Dictionary<string, string[]>
        {
            [RoleNames.SystemAdmin] = allPerms,
            [RoleNames.HospitalAdmin] = allPerms,
            [RoleNames.HospitalDirector] = [PermissionCodes.ReportRead, PermissionCodes.AuditLogRead, PermissionCodes.PatientRead, PermissionCodes.BillingRead, PermissionCodes.InpatientRead, PermissionCodes.AdminSystemStatusRead],
            [RoleNames.DepartmentHead] = [PermissionCodes.PatientRead, PermissionCodes.AppointmentRead, PermissionCodes.ClinicalRead, PermissionCodes.LabRead, PermissionCodes.ImagingRead, PermissionCodes.InpatientRead, PermissionCodes.NursingRead, PermissionCodes.ReportRead, PermissionCodes.HrManage],
            [RoleNames.Receptionist] = [PermissionCodes.PatientRead, PermissionCodes.PatientCreate, PermissionCodes.PatientUpdate, PermissionCodes.AppointmentRead, PermissionCodes.AppointmentManage, PermissionCodes.EncounterStart, PermissionCodes.QueueManage, PermissionCodes.BillingRead],
            [RoleNames.CustomerService] = [PermissionCodes.PatientRead, PermissionCodes.PatientCreate, PermissionCodes.AppointmentRead, PermissionCodes.AppointmentManage],
            [RoleNames.Doctor] = [PermissionCodes.PatientRead, PermissionCodes.AppointmentRead, PermissionCodes.ClinicalRead, PermissionCodes.ClinicalWrite, PermissionCodes.ClinicalNoteSign, PermissionCodes.ClinicalNoteAmend, PermissionCodes.ClinicalOrderCreate, PermissionCodes.PrescriptionCreate, PermissionCodes.LabRead, PermissionCodes.ImagingRead, PermissionCodes.InpatientRead, PermissionCodes.InpatientManage, PermissionCodes.NursingRead, PermissionCodes.SurgeryRead, PermissionCodes.SurgeryManage, PermissionCodes.EmergencyManage],
            [RoleNames.Nurse] = [PermissionCodes.PatientRead, PermissionCodes.ClinicalRead, PermissionCodes.InpatientRead, PermissionCodes.NursingRead, PermissionCodes.NursingWrite, PermissionCodes.EmergencyManage, PermissionCodes.LabRead, PermissionCodes.ImagingRead],
            [RoleNames.NurseManager] = [PermissionCodes.PatientRead, PermissionCodes.ClinicalRead, PermissionCodes.InpatientRead, PermissionCodes.InpatientManage, PermissionCodes.NursingRead, PermissionCodes.NursingWrite, PermissionCodes.HrManage, PermissionCodes.ReportRead],
            [RoleNames.LabTechnician] = [PermissionCodes.PatientRead, PermissionCodes.LabRead, PermissionCodes.LabSpecimenManage, PermissionCodes.LabResultEnter],
            [RoleNames.LabReviewer] = [PermissionCodes.PatientRead, PermissionCodes.LabRead, PermissionCodes.LabSpecimenManage, PermissionCodes.LabResultEnter, PermissionCodes.LabResultVerify, PermissionCodes.LabResultAmend],
            [RoleNames.RadiologyTechnician] = [PermissionCodes.PatientRead, PermissionCodes.ImagingRead, PermissionCodes.ImagingPerform],
            [RoleNames.Radiologist] = [PermissionCodes.PatientRead, PermissionCodes.ImagingRead, PermissionCodes.ImagingPerform, PermissionCodes.ImagingReportVerify],
            [RoleNames.Pharmacist] = [PermissionCodes.PatientRead, PermissionCodes.ClinicalRead, PermissionCodes.PharmacyRead, PermissionCodes.PharmacyDispense],
            [RoleNames.PharmacyManager] = [PermissionCodes.PatientRead, PermissionCodes.PharmacyRead, PermissionCodes.PharmacyDispense, PermissionCodes.PharmacyStockAdjust, PermissionCodes.InventoryManage, PermissionCodes.ReportRead],
            [RoleNames.Cashier] = [PermissionCodes.PatientRead, PermissionCodes.BillingRead, PermissionCodes.BillingChargeManage, PermissionCodes.BillingPaymentCollect, PermissionCodes.ReportRead],
            [RoleNames.Accountant] = [PermissionCodes.PatientRead, PermissionCodes.BillingRead, PermissionCodes.BillingChargeManage, PermissionCodes.BillingPaymentCollect, PermissionCodes.BillingRefundApprove, PermissionCodes.BillingPriceManage, PermissionCodes.ReportRead, PermissionCodes.AuditLogRead],
            [RoleNames.InventoryStaff] = [PermissionCodes.PharmacyRead, PermissionCodes.PharmacyStockAdjust, PermissionCodes.InventoryManage],
            [RoleNames.HRStaff] = [PermissionCodes.HrManage, PermissionCodes.AdminUserManage],
            [RoleNames.Auditor] = [PermissionCodes.AuditLogRead, PermissionCodes.IntegrationRead, PermissionCodes.ReportRead, PermissionCodes.PatientRead, PermissionCodes.BillingRead, PermissionCodes.ClinicalRead],
            [RoleNames.Patient] = [PermissionCodes.PortalAccess]
        };
    }
}
