using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using SmartHospital.Application.Admin;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Domain.Identity;
using SmartHospital.Infrastructure.Persistence;

namespace SmartHospital.Infrastructure.Identity;

public class CustomUserClaimsPrincipalFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<ApplicationRole> roleManager,
    IOptions<IdentityOptions> options,
    IUserAdminService userAdminService)
    : UserClaimsPrincipalFactory<ApplicationUser, ApplicationRole>(userManager, roleManager, options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("full_name", user.FullName));
        identity.AddClaim(new Claim("must_change_password", user.MustChangePassword ? "true" : "false"));
        if (user.DefaultFacilityId.HasValue)
        {
            identity.AddClaim(new Claim("default_facility_id", user.DefaultFacilityId.Value.ToString()));
        }
        if (user.DefaultDepartmentId.HasValue)
        {
            identity.AddClaim(new Claim("default_department_id", user.DefaultDepartmentId.Value.ToString()));
        }

        var permissions = await userAdminService.GetEffectivePermissionsAsync(user.Id);
        foreach (var perm in permissions)
        {
            identity.AddClaim(new Claim("permission", perm));
        }

        return identity;
    }
}

public class UserAdminService(
    HospitalDbContext db,
    UserManager<ApplicationUser> userManager,
    IAuditService auditService,
    ICurrentUserService currentUserService,
    IMemoryCache cache) : IUserAdminService
{
    private const string PermCachePrefix = "user_perms_";

    public async Task<PagedResult<UserListItemDto>> GetUsersAsync(PagedRequest request, CancellationToken cancellationToken = default)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(u =>
                (u.UserName != null && u.UserName.Contains(term)) ||
                u.FullName.Contains(term) ||
                (u.Email != null && u.Email.Contains(term)) ||
                (u.EmployeeCode != null && u.EmployeeCode.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.UserName)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var userRoles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where userIds.Contains(ur.UserId) && r.Name != null
            select new { ur.UserId, RoleName = r.Name! })
            .ToListAsync(cancellationToken);

        var roleLookup = userRoles.GroupBy(x => x.UserId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.RoleName).ToList());

        var now = DateTimeOffset.UtcNow;
        var items = users.Select(u => new UserListItemDto(
            u.Id,
            u.UserName ?? string.Empty,
            u.FullName,
            u.Email,
            u.EmployeeCode,
            u.IsActive,
            u.MustChangePassword,
            u.LockoutEnd.HasValue && u.LockoutEnd > now,
            roleLookup.GetValueOrDefault(u.Id, []),
            u.DefaultFacilityId,
            u.DefaultDepartmentId)).ToList();

        return new PagedResult<UserListItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<UserListItemDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var u = await db.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (u is null) return null;

        var roles = await (
            from ur in db.UserRoles.AsNoTracking()
            join r in db.Roles.AsNoTracking() on ur.RoleId equals r.Id
            where ur.UserId == id && r.Name != null
            select r.Name!).ToListAsync(cancellationToken);

        return new UserListItemDto(
            u.Id, u.UserName ?? string.Empty, u.FullName, u.Email, u.EmployeeCode,
            u.IsActive, u.MustChangePassword,
            u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
            roles, u.DefaultFacilityId, u.DefaultDepartmentId);
    }

    public async Task<(IReadOnlyList<Guid> FacilityIds, IReadOnlyList<Guid> DepartmentIds)> GetUserScopesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var facIds = await db.UserFacilityScopes.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => s.FacilityId)
            .ToListAsync(cancellationToken);
        var deptIds = await db.UserDepartmentScopes.AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => s.DepartmentId)
            .ToListAsync(cancellationToken);
        return (facIds, deptIds);
    }

    public async Task<OperationResult<Guid>> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.FullName))
            return OperationResult<Guid>.Fail("Tên đăng nhập và họ tên là bắt buộc.");

        // Prevent privilege escalation: non-SystemAdmin cannot assign SystemAdmin
        if (request.Roles.Contains(RoleNames.SystemAdmin) && !currentUserService.IsInRole(RoleNames.SystemAdmin))
            return OperationResult<Guid>.Fail("Bạn không có quyền gán vai trò SystemAdmin.");

        var user = new ApplicationUser
        {
            UserName = request.UserName.Trim(),
            FullName = request.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim(),
            EmailConfirmed = true,
            EmployeeCode = request.EmployeeCode?.Trim(),
            DefaultFacilityId = request.DefaultFacilityId,
            DefaultDepartmentId = request.DefaultDepartmentId,
            MustChangePassword = request.MustChangePassword,
            IsActive = true
        };

        var createRes = await userManager.CreateAsync(user, request.Password);
        if (!createRes.Succeeded)
        {
            return OperationResult<Guid>.Fail(string.Join("; ", createRes.Errors.Select(e => e.Description)));
        }

        if (request.Roles.Count > 0)
        {
            await userManager.AddToRolesAsync(user, request.Roles);
        }

        await SyncScopesAsync(user.Id, request.FacilityScopeIds, request.DepartmentScopeIds, request.DefaultFacilityId, cancellationToken);
        await auditService.LogAsync("UserCreated", "ApplicationUser", user.Id.ToString(), afterSummary: new { user.UserName, user.FullName, request.Roles }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(user.Id, "Đã tạo tài khoản người dùng thành công.");
    }

    public async Task<OperationResult> UpdateUserAsync(UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.Id.ToString());
        if (user is null) return OperationResult.Fail("Không tìm thấy người dùng.");

        if (request.Roles.Contains(RoleNames.SystemAdmin) && !currentUserService.IsInRole(RoleNames.SystemAdmin))
        {
            var currentRolesCheck = await userManager.GetRolesAsync(user);
            if (!currentRolesCheck.Contains(RoleNames.SystemAdmin))
                return OperationResult.Fail("Bạn không có quyền gán vai trò SystemAdmin.");
        }

        var before = new { user.FullName, user.IsActive, user.DefaultFacilityId, user.DefaultDepartmentId };

        user.FullName = request.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(request.Email) ? null : request.Email.Trim();
        user.EmployeeCode = request.EmployeeCode?.Trim();
        user.IsActive = request.IsActive;
        user.DefaultFacilityId = request.DefaultFacilityId;
        user.DefaultDepartmentId = request.DefaultDepartmentId;

        var updateRes = await userManager.UpdateAsync(user);
        if (!updateRes.Succeeded)
            return OperationResult.Fail(string.Join("; ", updateRes.Errors.Select(e => e.Description)));

        var existingRoles = await userManager.GetRolesAsync(user);
        var toRemove = existingRoles.Except(request.Roles).ToList();
        var toAdd = request.Roles.Except(existingRoles).ToList();

        if (toRemove.Count > 0) await userManager.RemoveFromRolesAsync(user, toRemove);
        if (toAdd.Count > 0) await userManager.AddToRolesAsync(user, toAdd);

        await SyncScopesAsync(user.Id, request.FacilityScopeIds, request.DepartmentScopeIds, request.DefaultFacilityId, cancellationToken);
        cache.Remove($"{PermCachePrefix}{user.Id}");

        await auditService.LogAsync("UserUpdated", "ApplicationUser", user.Id.ToString(),
            beforeSummary: before,
            afterSummary: new { user.FullName, user.IsActive, request.Roles },
            cancellationToken: cancellationToken);

        return OperationResult.Ok("Đã cập nhật thông tin người dùng.");
    }

    public async Task<OperationResult> ToggleUserActiveAsync(Guid userId, bool isActive, string reason, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Fail("Không tìm thấy người dùng.");
        if (string.Equals(user.UserName, "admin", StringComparison.OrdinalIgnoreCase) && !isActive)
            return OperationResult.Fail("Không thể vô hiệu hóa tài khoản admin gốc.");

        user.IsActive = isActive;
        if (!isActive)
        {
            await userManager.UpdateSecurityStampAsync(user);
        }
        await userManager.UpdateAsync(user);
        cache.Remove($"{PermCachePrefix}{user.Id}");

        await auditService.LogAsync(isActive ? "UserActivated" : "UserDeactivated", "ApplicationUser", user.Id.ToString(), reason: reason, cancellationToken: cancellationToken);
        return OperationResult.Ok(isActive ? "Đã kích hoạt lại tài khoản." : "Đã vô hiệu hóa tài khoản.");
    }

    public async Task<OperationResult> ResetPasswordAsync(Guid userId, string newPassword, bool mustChangePassword, string reason, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user is null) return OperationResult.Fail("Không tìm thấy người dùng.");

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var res = await userManager.ResetPasswordAsync(user, token, newPassword);
        if (!res.Succeeded)
            return OperationResult.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));

        user.MustChangePassword = mustChangePassword;
        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);

        await auditService.LogAsync("UserPasswordReset", "ApplicationUser", user.Id.ToString(), reason: reason, cancellationToken: cancellationToken);
        return OperationResult.Ok("Đã đặt lại mật khẩu thành công.");
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await cache.GetOrCreateAsync($"{PermCachePrefix}{userId}", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
            var perms = await (
                from ur in db.UserRoles.AsNoTracking()
                join rp in db.RolePermissions.AsNoTracking() on ur.RoleId equals rp.RoleId
                join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
                where ur.UserId == userId
                select p.Code)
                .Distinct()
                .ToListAsync(cancellationToken);

            return (IReadOnlyList<string>)perms;
        }) ?? [];
    }

    private async Task SyncScopesAsync(
        Guid userId,
        IReadOnlyList<Guid> facilityIds,
        IReadOnlyList<Guid> departmentIds,
        Guid? defaultFacilityId,
        CancellationToken cancellationToken)
    {
        var existingFac = await db.UserFacilityScopes.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        db.UserFacilityScopes.RemoveRange(existingFac);
        foreach (var fId in facilityIds.Distinct())
        {
            db.UserFacilityScopes.Add(new UserFacilityScope
            {
                UserId = userId,
                FacilityId = fId,
                IsDefault = defaultFacilityId == fId
            });
        }

        var existingDept = await db.UserDepartmentScopes.Where(s => s.UserId == userId).ToListAsync(cancellationToken);
        db.UserDepartmentScopes.RemoveRange(existingDept);

        if (departmentIds.Count > 0)
        {
            var depts = await db.Departments.AsNoTracking()
                .Where(d => departmentIds.Contains(d.Id))
                .Select(d => new { d.Id, d.FacilityId })
                .ToListAsync(cancellationToken);

            foreach (var d in depts)
            {
                db.UserDepartmentScopes.Add(new UserDepartmentScope
                {
                    UserId = userId,
                    FacilityId = d.FacilityId,
                    DepartmentId = d.Id
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}

public class RoleAdminService(
    HospitalDbContext db,
    RoleManager<ApplicationRole> roleManager,
    IAuditService auditService,
    IMemoryCache cache) : IRoleAdminService
{
    public async Task<IReadOnlyList<RoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await roleManager.Roles.AsNoTracking().OrderBy(r => r.Name).ToListAsync(cancellationToken);
        var rolePerms = await (
            from rp in db.RolePermissions.AsNoTracking()
            join p in db.Permissions.AsNoTracking() on rp.PermissionId equals p.Id
            select new { rp.RoleId, p.Code }).ToListAsync(cancellationToken);

        var lookup = rolePerms.GroupBy(x => x.RoleId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Code).OrderBy(c => c).ToList());

        return roles.Select(r => new RoleDto(
            r.Id, r.Name ?? string.Empty, r.DisplayName, r.Description, r.IsSystemRole,
            lookup.GetValueOrDefault(r.Id, []))).ToList();
    }

    public async Task<IReadOnlyList<PermissionDto>> GetAllPermissionsAsync(CancellationToken cancellationToken = default)
        => await db.Permissions.AsNoTracking()
            .OrderBy(p => p.Module).ThenBy(p => p.Code)
            .Select(p => new PermissionDto(p.Id, p.Code, p.Module, p.Name))
            .ToListAsync(cancellationToken);

    public async Task<OperationResult> UpdateRolePermissionsAsync(Guid roleId, IReadOnlyList<string> permissionCodes, CancellationToken cancellationToken = default)
    {
        var role = await roleManager.FindByIdAsync(roleId.ToString());
        if (role is null) return OperationResult.Fail("Không tìm thấy vai trò.");

        var perms = await db.Permissions
            .Where(p => permissionCodes.Contains(p.Code))
            .ToListAsync(cancellationToken);

        var existing = await db.RolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync(cancellationToken);
        db.RolePermissions.RemoveRange(existing);

        foreach (var p in perms)
        {
            db.RolePermissions.Add(new RolePermission { RoleId = roleId, PermissionId = p.Id });
        }
        await db.SaveChangesAsync(cancellationToken);

        // Invalidate permission cache for all users in this role
        var userIds = await db.UserRoles.Where(ur => ur.RoleId == roleId).Select(ur => ur.UserId).ToListAsync(cancellationToken);
        foreach (var uid in userIds)
        {
            cache.Remove($"user_perms_{uid}");
        }

        await auditService.LogAsync("RolePermissionsUpdated", "ApplicationRole", roleId.ToString(),
            afterSummary: new { role.Name, PermissionCount = perms.Count },
            cancellationToken: cancellationToken);

        return OperationResult.Ok($"Đã cập nhật {perms.Count} quyền cho vai trò {role.DisplayName}.");
    }

    public async Task<OperationResult<Guid>> CreateRoleAsync(string name, string displayName, string? description, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(displayName))
            return OperationResult<Guid>.Fail("Mã vai trò và tên hiển thị là bắt buộc.");

        var role = new ApplicationRole
        {
            Name = name.Trim(),
            DisplayName = displayName.Trim(),
            Description = description?.Trim(),
            IsSystemRole = false
        };
        var res = await roleManager.CreateAsync(role);
        if (!res.Succeeded)
            return OperationResult<Guid>.Fail(string.Join("; ", res.Errors.Select(e => e.Description)));

        await auditService.LogAsync("RoleCreated", "ApplicationRole", role.Id.ToString(), afterSummary: new { role.Name, role.DisplayName }, cancellationToken: cancellationToken);
        return OperationResult<Guid>.Ok(role.Id, "Đã tạo vai trò mới.");
    }
}

public class AuditQueryService(HospitalDbContext db) : IAuditQueryService
{
    public async Task<PagedResult<AuditLogItemDto>> GetAuditLogsAsync(
        PagedRequest request,
        string? entityType = null,
        string? action = null,
        DateOnly? fromDate = null,
        DateOnly? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.AuditLogs.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(a => a.UserName.Contains(term) || a.EntityId.Contains(term) || (a.Reason != null && a.Reason.Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(a => a.EntityType == entityType);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(a => a.Action == action);
        if (fromDate.HasValue)
        {
            var startUtc = new DateTimeOffset(fromDate.Value.ToDateTime(TimeOnly.MinValue), TimeSpan.FromHours(7)).ToUniversalTime();
            query = query.Where(a => a.CreatedAtUtc >= startUtc);
        }
        if (toDate.HasValue)
        {
            var endUtc = new DateTimeOffset(toDate.Value.ToDateTime(TimeOnly.MaxValue), TimeSpan.FromHours(7)).ToUniversalTime();
            query = query.Where(a => a.CreatedAtUtc <= endUtc);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(a => a.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(a => new AuditLogItemDto(
                a.Id, a.CreatedAtUtc, a.UserName, a.Action, a.EntityType, a.EntityId,
                a.Reason, a.CorrelationId, a.IpAddress, a.BeforeSummaryJson, a.AfterSummaryJson))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogItemDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<PagedResult<IntegrationMessageDto>> GetIntegrationMessagesAsync(
        PagedRequest request,
        string? integrationType = null,
        CancellationToken cancellationToken = default)
    {
        var query = db.IntegrationMessages.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(integrationType))
            query = query.Where(m => m.IntegrationType == integrationType);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(m => m.ProviderName.Contains(term) || m.IdempotencyKey.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(m => m.CreatedAtUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(m => new IntegrationMessageDto(
                m.Id, m.CreatedAtUtc, m.ProviderName, m.IntegrationType, m.Direction.ToString(),
                m.Status.ToString(), m.IdempotencyKey, m.CorrelationId, m.Endpoint,
                m.RequestSummary, m.ResponseSummary, m.ErrorMessage))
            .ToListAsync(cancellationToken);

        return new PagedResult<IntegrationMessageDto>
        {
            Items = items,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
