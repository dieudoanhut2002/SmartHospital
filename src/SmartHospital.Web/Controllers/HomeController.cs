using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.MasterData;
using SmartHospital.Domain.Common;

namespace SmartHospital.Web.Controllers;

[Authorize]
public class HomeController(
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser,
    IScopeAuthorizationService scopeAuth,
    IHospitalDbContext db) : Controller
{
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (currentUser.IsInRole(RoleNames.Patient) && !currentUser.IsInRole(RoleNames.SystemAdmin))
        {
            return RedirectToAction("Index", "Portal", new { area = "Portal" });
        }

        var facilities = await masterDataService.GetFacilitiesAsync(cancellationToken);
        var activeFacilityId = currentUser.ActiveFacilityId ?? facilities.FirstOrDefault()?.Id ?? Guid.Empty;

        var metrics = activeFacilityId != Guid.Empty
            ? await opsService.GetDashboardMetricsAsync(activeFacilityId, cancellationToken)
            : new DashboardMetricsDto(0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

        ViewBag.Facilities = facilities;
        ViewBag.ActiveFacilityId = activeFacilityId;

        var notifications = await db.Notifications.AsNoTracking()
            .Where(n => n.RecipientUserId == currentUser.UserId || n.RecipientUserId == null)
            .OrderByDescending(n => n.CreatedAtUtc)
            .Take(8)
            .ToListAsync(cancellationToken);
        ViewBag.Notifications = notifications;

        return View(metrics);
    }

    [HttpPost]
    public async Task<IActionResult> SwitchFacility(Guid facilityId, string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? Guid.Empty;
        if (!await scopeAuth.CanAccessFacilityAsync(userId, facilityId, cancellationToken))
        {
            TempData["Error"] = "Bạn không được cấp quyền truy cập cơ sở này.";
            return RedirectToAction(nameof(Index));
        }

        HttpContext.Session.SetString("ActiveFacilityId", facilityId.ToString());
        TempData["Success"] = "Đã chuyển cơ sở làm việc.";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    [AllowAnonymous]
    public IActionResult Error(string? message = null)
    {
        ViewBag.ErrorMessage = message ?? "Đã xảy ra lỗi trong quá trình xử lý yêu cầu.";
        ViewBag.CorrelationId = HttpContext.TraceIdentifier;
        return View();
    }

    [AllowAnonymous]
    public IActionResult HttpStatus(int code)
    {
        ViewBag.StatusCode = code;
        return code == 403 ? View("AccessDenied") : View("NotFoundPage");
    }
}
