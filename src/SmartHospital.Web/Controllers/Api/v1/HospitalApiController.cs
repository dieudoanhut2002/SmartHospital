using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartHospital.Application.Common.Interfaces;
using SmartHospital.Application.Inpatient;
using SmartHospital.Application.MasterData;
using SmartHospital.Application.Patients;
using SmartHospital.Application.Reception;
using SmartHospital.Contracts.Common;
using SmartHospital.Domain.Common;
using SmartHospital.Web.Security;

namespace SmartHospital.Web.Controllers.Api.v1;

[ApiController]
[Authorize]
[IgnoreAntiforgeryToken]
[Route("api/v1")]
public class HospitalApiController(
    IPatientService patientService,
    IReceptionService receptionService,
    IInpatientAndHospitalOpsService opsService,
    IMasterDataService masterDataService,
    ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet("patients")]
    [HasPermission(PermissionCodes.PatientRead)]
    public async Task<IActionResult> SearchPatients([FromQuery] PagedRequest request, CancellationToken cancellationToken)
    {
        var result = await patientService.SearchPatientsAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("patients/{id:guid}")]
    [HasPermission(PermissionCodes.PatientRead)]
    public async Task<IActionResult> GetPatient(Guid id, CancellationToken cancellationToken)
    {
        var patient = await patientService.GetPatientDetailAsync(id, cancellationToken);
        if (patient is null)
            return NotFound(new ApiErrorResponse("NOT_FOUND", "Không tìm thấy hồ sơ bệnh nhân.", HttpContext.TraceIdentifier));
        return Ok(patient);
    }

    [HttpGet("queue")]
    [HasPermission(PermissionCodes.AppointmentRead)]
    public async Task<IActionResult> GetQueue([FromQuery] Guid? facilityId, [FromQuery] Guid? clinicId, CancellationToken cancellationToken)
    {
        var facId = facilityId ?? currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        var board = await receptionService.GetQueueBoardAsync(facId, clinicId, null, cancellationToken);
        return Ok(board);
    }

    [HttpGet("dashboard/metrics")]
    [HasPermission(PermissionCodes.ReportRead)]
    public async Task<IActionResult> GetDashboardMetrics([FromQuery] Guid? facilityId, CancellationToken cancellationToken)
    {
        var facId = facilityId ?? currentUser.ActiveFacilityId ?? (await masterDataService.GetFacilitiesAsync(cancellationToken)).FirstOrDefault()?.Id ?? Guid.Empty;
        var metrics = await opsService.GetDashboardMetricsAsync(facId, cancellationToken);
        return Ok(metrics);
    }
}
