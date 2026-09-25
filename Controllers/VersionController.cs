using LocalMock.Models;
using LocalMock.Services;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalMock.Controllers;

[ApiController]
[Route("api")]
[Produces("application/json")]
public class VersionController : ControllerBase
{
    private readonly IAppUpdateService _appUpdateService;

    public VersionController(IAppUpdateService appUpdateService)
    {
        _appUpdateService = appUpdateService;
    }

    /// <summary>
    /// Returns the current version and whether an update is available on GitHub Releases.
    /// </summary>
    [HttpGet("version")]
    [SwaggerOperation(
        Summary = "Version status",
        Description = "Compares the installed version with the latest GitHub release",
        OperationId = "GetVersionStatus",
        Tags = new[] { "Version" })]
    [SwaggerResponse(200, "Status retrieved")]
    public async Task<ActionResult<VersionStatusResponse>> GetVersion(CancellationToken cancellationToken)
    {
        var status = await _appUpdateService.GetVersionStatusAsync(cancellationToken);
        return Ok(new VersionStatusResponse
        {
            CurrentVersion = status.CurrentVersion,
            LatestVersion = status.LatestVersion,
            UpdateAvailable = status.UpdateAvailable,
            ReleaseUrl = status.ReleaseUrl,
            ReleaseNotes = status.ReleaseNotes,
            Error = status.Error
        });
    }

    /// <summary>
    /// Starts a full update from the GitHub Release (replaces binaries and restarts the service).
    /// </summary>
    [HttpPost("update")]
    [SwaggerOperation(
        Summary = "Update application",
        Description = "Downloads the latest release asset and applies the update by restarting the service",
        OperationId = "StartUpdate",
        Tags = new[] { "Version" })]
    [SwaggerResponse(202, "Update started")]
    [SwaggerResponse(400, "No update available")]
    [SwaggerResponse(409, "Update already in progress")]
    public async Task<ActionResult<UpdateStartResponse>> StartUpdate(CancellationToken cancellationToken)
    {
        var result = await _appUpdateService.StartUpdateAsync(cancellationToken);
        var body = new UpdateStartResponse
        {
            Started = result.Started,
            Message = result.Message,
            TargetVersion = result.TargetVersion
        };

        return StatusCode(result.StatusCode, body);
    }
}
