using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Organization;

public class Hospital : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? TaxCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string TimeZoneId { get; set; } = "Asia/Ho_Chi_Minh";
    public string CurrencyCode { get; set; } = "VND";

    public ICollection<Facility> Facilities { get; set; } = [];
}

public class Facility : AuditableEntity
{
    public Guid HospitalId { get; set; }
    public Hospital? Hospital { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsDefault { get; set; }

    public ICollection<Department> Departments { get; set; } = [];
    public ICollection<Room> Rooms { get; set; } = [];
    public ICollection<Clinic> Clinics { get; set; } = [];
}

public class Department : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DepartmentType DepartmentType { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }

    public ICollection<Room> Rooms { get; set; } = [];
    public ICollection<Clinic> Clinics { get; set; } = [];
}

public class Specialty : AuditableEntity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class Room : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public RoomType RoomType { get; set; }
    public string? Floor { get; set; }

    public ICollection<Bed> Beds { get; set; } = [];
}

public class Clinic : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public Guid DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public Guid? RoomId { get; set; }
    public Room? Room { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string QueuePrefix { get; set; } = "K";
}

public class Bed : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid RoomId { get; set; }
    public Room? Room { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public BedStatus Status { get; set; } = BedStatus.Available;
    public decimal DailyRate { get; set; }
}

public class Employee : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
    public Guid? UserId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public DateOnly? HireDate { get; set; }
    public bool IsTerminated { get; set; }

    public DoctorProfile? DoctorProfile { get; set; }
    public NurseProfile? NurseProfile { get; set; }
}

public class DoctorProfile : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? SpecialtyId { get; set; }
    public Specialty? Specialty { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Degree { get; set; }
}

public class NurseProfile : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public string? LicenseNumber { get; set; }
    public string? Level { get; set; }
}

public class ShiftSchedule : AuditableEntity
{
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public Guid? ClinicId { get; set; }
    public DateOnly ShiftDate { get; set; }
    public string ShiftName { get; set; } = string.Empty; // Sáng / Chiều / Đêm
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public string? Notes { get; set; }
}

public class LeaveRecord : AuditableEntity
{
    public Guid EmployeeId { get; set; }
    public Employee? Employee { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public string LeaveType { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Approved";
}
