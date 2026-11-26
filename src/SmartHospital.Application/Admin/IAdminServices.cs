using SmartHospital.Contracts.Common;

namespace SmartHospital.Application.Admin;

public record UserListItemDto(
    Guid Id,
    string UserName,
    string FullName,
    string? Email,
    string? EmployeeCode,
    bool IsActive,
    bool MustChangePassword,
    bool IsLockedOut,
    IReadOnlyList<string> Roles,
    Guid? DefaultFacilityId,
    Guid? DefaultDepartmentId);

public record CreateUserRequest(
    string UserName,
    string FullName,
    string? Email,
    string Password,
    string? EmployeeCode,
    Guid? DefaultFacilityId,
    Guid? DefaultDepartmentId,
    bool MustChangePassword,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> FacilityScopeIds,
    IReadOnlyList<Guid> DepartmentScopeIds);

public record UpdateUserRequest(
    Guid Id,
    string FullName,
    string? Email,
    string? EmployeeCode,
    bool IsActive,
    Guid? DefaultFacilityId,
    Guid? DefaultDepartmentId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<Guid> FacilityScopeIds,
    IReadOnlyList<Guid> DepartmentScopeIds);

public record RoleDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    bool IsSystemRole,
    IReadOnlyList<string> PermissionCodes);

public record PermissionDto(Guid Id, string Code, string Module, string Name);

public record AuditLogItemDto(
    long Id,
    DateTimeOffset CreatedAtUtc,
    string UserName,
    string Action,
    string EntityType,
    string EntityId,
    string? Reason,
    string? CorrelationId,
    string? IpAddress,
    string? BeforeSummaryJson,
    string? AfterSummaryJson);

public record IntegrationMessageDto(
    long Id,
    DateTimeOffset CreatedAtUtc,
    string ProviderName,
    string IntegrationType,
    string Direction,
    string Status,
    string IdempotencyKey,
    string? CorrelationId,
    string? Endpoint,
    string? RequestSummary,
    string? ResponseSummary,
    string? ErrorMessage);

public interface IUserAdminService
{
    Task<PagedResult<UserListItemDto>> GetUsersAsync(PagedRequest request, CancellationToken cancellationToken = default);
    Task<UserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Guid> FacilityIds, IReadOnlyList<Guid> DepartmentIds)> GetUserScopesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default);
    Task<OperationResult> ToggleUserActiveAsync(Guid userId, bool isActive, string reason, CancellationToken cancellationToken = default);
    Task<OperationResult> ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IRoleAdminService
{
    Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default);
    Task<OperationResult> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default);
    Task<OperationResult<Guid>> CreateRoleAsync(string name, string displayName, string? description, CancellationToken cancellationToken = default);
}

public interface IAuditQueryService
{
    Task<PagedResult<AuditLogItemDto>> GetAuditLogsAsync(
        PagedRequest request,
        string? entityType = null,
        string? action = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default);

    Task<PagedResult<IntegrationMessageDto>> GetIntegrationMessagesAsync(
        PagedRequest request,
        string? integrationType = null,
        CancellationToken cancellationToken = default);
}
