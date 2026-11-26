using SmartHospital.Domain.Common;

namespace SmartHospital.Domain.Identity;

public class Permission : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class RolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
    public Permission? Permission { get; set; }
}

public class UserFacilityScope : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid FacilityId { get; set; }
    public bool IsDefault { get; set; }
}

public class UserDepartmentScope : AuditableEntity
{
    public Guid UserId { get; set; }
    public Guid FacilityId { get; set; }
    public Guid DepartmentId { get; set; }
    public Guid? ClinicId { get; set; }
    public Guid? WarehouseId { get; set; }
}
