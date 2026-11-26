using Microsoft.AspNetCore.Identity;

namespace SmartHospital.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public bool MustChangePassword { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastPasswordChangedAtUtc { get; set; }
    public Guid? DefaultFacilityId { get; set; }
    public Guid? DefaultDepartmentId { get; set; }
    public string? EmployeeCode { get; set; }
}

public class ApplicationRole : IdentityRole<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
}
