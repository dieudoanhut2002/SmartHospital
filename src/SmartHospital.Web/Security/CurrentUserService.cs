using System.Security.Claims;
using SmartHospital.Application.Admin;
using SmartHospital.Application.Common.Interfaces;

namespace SmartHospital.Web.Security;

public class CurrentUserService(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider) : ICurrentUserService
{
    private IReadOnlyCollection<string>? _cachedPermissions;

    public Guid? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(raw, out var id) ? id : null;
        }
    }

    public string UserName
        => httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "anonymous";

    public string FullName
        => httpContextAccessor.HttpContext?.User?.FindFirstValue("full_name") ?? UserName;

    public bool IsAuthenticated
        => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? ActiveFacilityId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx?.Session is not null)
            {
                var sessionVal = ctx.Session.GetString("ActiveFacilityId");
                if (Guid.TryParse(sessionVal, out var fid)) return fid;
            }
            var claimVal = ctx?.User?.FindFirstValue("default_facility_id");
            return Guid.TryParse(claimVal, out var cfid) ? cfid : null;
        }
    }

    public Guid? ActiveDepartmentId
    {
        get
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx?.Session is not null)
            {
                var sessionVal = ctx.Session.GetString("ActiveDepartmentId");
                if (Guid.TryParse(sessionVal, out var did)) return did;
            }
            var claimVal = ctx?.User?.FindFirstValue("default_department_id");
            return Guid.TryParse(claimVal, out var cdid) ? cdid : null;
        }
    }

    public string? CorrelationId
        => httpContextAccessor.HttpContext?.TraceIdentifier;

    public string? IpAddress
        => httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    public string? UserAgent
        => httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();

    public IReadOnlyCollection<string> Roles
        => httpContextAccessor.HttpContext?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList() ?? [];

    public bool IsInRole(string roleName)
        => httpContextAccessor.HttpContext?.User?.IsInRole(roleName) ?? false;

    public bool HasPermission(string permissionCode)
    {
        if (!IsAuthenticated) return false;

        if (_cachedPermissions is null)
        {
            if (UserId.HasValue)
            {
                var adminSvc = serviceProvider.GetService<IUserAdminService>();
                if (adminSvc is not null)
                {
                    _cachedPermissions = adminSvc.GetEffectivePermissionsAsync(UserId.Value).GetAwaiter().GetResult();
                }
            }
            _cachedPermissions ??= httpContextAccessor.HttpContext?.User?.FindAll("permission").Select(c => c.Value).ToHashSet() ?? [];
        }

        return _cachedPermissions.Contains(permissionCode);
    }
}
