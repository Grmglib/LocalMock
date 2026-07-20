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
    /// Retorna a versão atual e se há atualização disponível no GitHub Releases.
    /// </summary>
    [HttpGet("version")]
    [SwaggerOperation(
        Summary = "Status de versão",
        Description = "Compara a versão instalada com o latest release do GitHub",
        OperationId = "GetVersionStatus",
        Tags = new[] { "Version" })]
    [SwaggerResponse(200, "Status obtido")]
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
    /// Inicia a atualização completa a partir do GitHub Release (substitui binários e reinicia o serviço).
    /// </summary>
    [HttpPost("update")]
    [SwaggerOperation(
        Summary = "Atualizar aplicação",
        Description = "Baixa o asset do latest release e aplica a atualização reiniciando o serviço",
        OperationId = "StartUpdate",
        Tags = new[] { "Version" })]
    [SwaggerResponse(202, "Atualização iniciada")]
    [SwaggerResponse(400, "Nenhuma atualização disponível")]
    [SwaggerResponse(409, "Atualização já em andamento")]
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
