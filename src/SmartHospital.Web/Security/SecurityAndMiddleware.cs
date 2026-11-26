using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Serilog.Context;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Infrastructure.Identity;

namespace SmartHospital.Web.Security;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
public class HasPermissionAttribute(string permission) : AuthorizeAttribute(policy: $"{PolicyPrefix}{permission}")
{
    public const string PolicyPrefix = "Permission:";
}

public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}

public class PermissionAuthorizationHandler(ICurrentUserService currentUserService)
    : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (currentUserService.HasPermission(requirement.Permission))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}

public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
    : DefaultAuthorizationPolicyProvider(options)
{
    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            var permission = policyName[HasPermissionAttribute.PolicyPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return base.GetPolicyAsync(policyName);
    }
}

public class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string HeaderName = "X-Correlation-ID";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N")[..16];
        }

        context.TraceIdentifier = correlationId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}

public class MustChangePasswordMiddleware(RequestDelegate next)
{
    private static readonly string[] AllowedPrefixes =
    [
        "/Account/ChangePassword",
        "/Account/Logout",
        "/Account/Login",
        "/health",
        "/css/",
        "/js/",
        "/lib/",
        "/favicon.ico"
    ];

    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (!AllowedPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                var userId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user is not null && (!user.IsActive || user.MustChangePassword))
                    {
                        if (!user.IsActive)
                        {
                            context.Response.Redirect("/Account/Logout");
                            return;
                        }
                        context.Response.Redirect("/Account/ChangePassword?forced=true");
                        return;
                    }
                }
            }
        }

        await next(context);
    }
}

public class GlobalExceptionHandlerMiddleware(
    RequestDelegate next,
    ILogger<GlobalExceptionHandlerMiddleware> logger,
    IHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DomainException ex)
        {
            logger.LogWarning(ex, "Domain rule violation at {Path} (CorrelationId: {CorrelationId})",
                context.Request.Path, context.TraceIdentifier);

            if (IsApiRequest(context))
            {
                await WriteJsonErrorAsync(context, HttpStatusCode.BadRequest, "DOMAIN_ERROR", ex.Message);
            }
            else
            {
                context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(ex.Message)}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception at {Path} (CorrelationId: {CorrelationId})",
                context.Request.Path, context.TraceIdentifier);

            var userMsg = environment.IsDevelopment()
                ? $"Lỗi hệ thống: {ex.Message}"
                : $"Đã xảy ra lỗi hệ thống. Mã tra cứu: {context.TraceIdentifier}.";

            if (IsApiRequest(context))
            {
                await WriteJsonErrorAsync(context, HttpStatusCode.InternalServerError, "INTERNAL_ERROR", userMsg);
            }
            else
            {
                context.Response.Redirect($"/Home/Error?message={Uri.EscapeDataString(userMsg)}");
            }
        }
    }

    private static bool IsApiRequest(HttpContext context)
        => context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase);

    private static async Task WriteJsonErrorAsync(HttpContext context, HttpStatusCode statusCode, string code, string message)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        var payload = new ApiErrorResponse(code, message, context.TraceIdentifier);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
