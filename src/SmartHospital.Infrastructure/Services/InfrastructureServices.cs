using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Domain.Audit;
using SmartHospital.Domain.Common;
using SmartHospital.Infrastructure.Persistence;

namespace SmartHospital.Infrastructure.Services;

public class AuditService(
    HospitalDbContext db,
    ICurrentUserService currentUserService) : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(
        string action,
        string entityType,
        string entityId,
        string? reason = null,
        object? beforeSummary = null,
        object? afterSummary = null,
        Guid? facilityId = null,
        Guid? departmentId = null,
        CancellationToken cancellationToken = default)
    {
        var log = new AuditLog
        {
            UserId = currentUserService.UserId,
            UserName = string.IsNullOrWhiteSpace(currentUserService.UserName) ? "system" : currentUserService.UserName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            FacilityId = facilityId ?? currentUserService.ActiveFacilityId,
            DepartmentId = departmentId ?? currentUserService.ActiveDepartmentId,
            CorrelationId = currentUserService.CorrelationId,
            IpAddress = currentUserService.IpAddress,
            UserAgent = Truncate(currentUserService.UserAgent, 300),
            Reason = reason,
            BeforeSummaryJson = SerializeSafe(beforeSummary),
            AfterSummaryJson = SerializeSafe(afterSummary),
            CreatedAtUtc = DateTimeOffset.UtcNow
        };

        db.AuditLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static string? SerializeSafe(object? data)
    {
        if (data is null) return null;
        var json = JsonSerializer.Serialize(data, JsonOptions);
        return json.Length > 4000 ? json[..4000] : json;
    }

    private static string? Truncate(string? value, int maxLen)
        => string.IsNullOrEmpty(value) ? value : value.Length <= maxLen ? value : value[..maxLen];
}

public class ScopeAuthorizationService(
    HospitalDbContext db,
    ICurrentUserService currentUserService) : IScopeAuthorizationService
{
    private static readonly HashSet<string> HospitalWideRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        RoleNames.SystemAdmin,
        RoleNames.HospitalAdmin,
        RoleNames.HospitalDirector,
        RoleNames.Auditor
    };

    public async Task<bool> HasHospitalWideScopeAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (currentUserService.UserId == userId &&
            currentUserService.Roles.Any(r => HospitalWideRoles.Contains(r)))
        {
            return true;
        }

        var roleNames = await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId && r.Name != null
            select r.Name!).ToListAsync(cancellationToken);

        return roleNames.Any(r => HospitalWideRoles.Contains(r));
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedFacilityIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (await HasHospitalWideScopeAsync(userId, cancellationToken))
        {
            return await db.Facilities
                .Where(f => f.IsActive)
                .Select(f => f.Id)
                .ToListAsync(cancellationToken);
        }

        return await db.UserFacilityScopes
            .Where(s => s.UserId == userId && s.IsActive)
            .Select(s => s.FacilityId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAllowedDepartmentIdsAsync(Guid userId, Guid? facilityId = null, CancellationToken cancellationToken = default)
    {
        if (await HasHospitalWideScopeAsync(userId, cancellationToken))
        {
            var q = db.Departments.Where(d => d.IsActive);
            if (facilityId.HasValue) q = q.Where(d => d.FacilityId == facilityId.Value);
            return await q.Select(d => d.Id).ToListAsync(cancellationToken);
        }

        var query = db.UserDepartmentScopes
            .Where(s => s.UserId == userId && s.IsActive);

        if (facilityId.HasValue)
        {
            query = query.Where(s => s.FacilityId == facilityId.Value);
        }

        return await query.Select(s => s.DepartmentId).Distinct().ToListAsync(cancellationToken);
    }

    public async Task<bool> CanAccessFacilityAsync(Guid userId, Guid facilityId, CancellationToken cancellationToken = default)
    {
        if (await HasHospitalWideScopeAsync(userId, cancellationToken)) return true;

        return await db.UserFacilityScopes.AnyAsync(
            s => s.UserId == userId && s.FacilityId == facilityId && s.IsActive,
            cancellationToken);
    }

    public async Task<bool> CanAccessDepartmentAsync(Guid userId, Guid facilityId, Guid departmentId, CancellationToken cancellationToken = default)
    {
        if (await HasHospitalWideScopeAsync(userId, cancellationToken)) return true;

        var hasSpecificDept = await db.UserDepartmentScopes.AnyAsync(
            s => s.UserId == userId && s.FacilityId == facilityId && s.DepartmentId == departmentId && s.IsActive,
            cancellationToken);
        if (hasSpecificDept) return true;

        // If user has facility scope and no specific department restriction, allow facility-level roles
        var hasAnyDeptScope = await db.UserDepartmentScopes.AnyAsync(s => s.UserId == userId && s.IsActive, cancellationToken);
        if (!hasAnyDeptScope)
        {
            return await CanAccessFacilityAsync(userId, facilityId, cancellationToken);
        }

        return false;
    }

    public async Task<bool> CanAccessWarehouseAsync(Guid userId, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        if (await HasHospitalWideScopeAsync(userId, cancellationToken)) return true;

        var warehouse = await db.Warehouses.AsNoTracking().FirstOrDefaultAsync(w => w.Id == warehouseId, cancellationToken);
        if (warehouse is null) return false;

        var hasWhScope = await db.UserDepartmentScopes.AnyAsync(
            s => s.UserId == userId && s.WarehouseId == warehouseId && s.IsActive,
            cancellationToken);
        if (hasWhScope) return true;

        return await CanAccessFacilityAsync(userId, warehouse.FacilityId, cancellationToken);
    }
}

public class CodeGeneratorService : ICodeGeneratorService
{
    public Task<string> GenerateNextCodeAsync(string prefix, string sequenceKey, CancellationToken cancellationToken = default)
    {
        var vnNow = HospitalTime.ToVietnamTime(DateTimeOffset.UtcNow);
        var suffix = RandomNumberGenerator.GetInt32(1000, 9999);
        var code = $"{prefix}-{vnNow:yyMMddHHmmss}-{suffix}";
        return Task.FromResult(code);
    }
}
