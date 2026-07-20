using LocalMock.Models;
using LocalMock.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalMock.Controllers
{
    /// <summary>
    /// Serviço de mock para os endpoints da API.
    /// Permite criar, listar e servir respostas mockadas.
    /// </summary>
    [ApiController]
    [Route("mock")]
    [Produces("application/json")]
    public class MockController : ControllerBase
    {
        private readonly IMockService _mockService;
        private readonly ICollectionService _collectionService;
        private readonly IBypassProxyService _bypassProxyService;

        public MockController(
            IMockService mockService,
            ICollectionService collectionService,
            IBypassProxyService bypassProxyService)
        {
            _mockService = mockService;
            _collectionService = collectionService;
            _bypassProxyService = bypassProxyService;
        }

        /// <summary>
        /// Cria ou atualiza um mock para um endpoint.
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Cria mock", Description = "Registra um mock para um método e path da API", OperationId = "CriarMock", Tags = new[] { "Mock" })]
        [SwaggerResponse(201, "Mock criado com sucesso")]
        [SwaggerResponse(400, "Requisição inválida")]
        public async Task<IActionResult> Create([FromBody] CreateMockRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Method) || string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Method e Path são obrigatórios.");
            }

            if (request.BypassEnabled && !IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl deve ser uma URL absoluta HTTP/HTTPS quando o bypass estiver ativo.");
            }

            if (request.ResponseDelayMs < 0)
            {
                return BadRequest("ResponseDelayMs deve ser maior ou igual a zero.");
            }

            var collection = MockStorePersistence.NormalizeCollectionReference(request.Collection);
            if (!string.IsNullOrWhiteSpace(collection))
            {
                if (!MockStorePersistence.IsValidCollectionId(collection))
                {
                    return BadRequest("Collection deve ser um slug válido (letras, números, _ e -).");
                }

                if (!await _collectionService.ExistsAsync(collection, cancellationToken))
                {
                    return BadRequest($"Coleção '{collection}' não encontrada.");
                }
            }

            await _mockService.AddAsync(
                collection,
                request.Method.Trim(),
                request.Path,
                request.StatusCode,
                request.ResponseDelayMs,
                request.ResponseBody,
                request.ResponseContentType,
                request.Enabled,
                request.BypassEnabled,
                request.BypassUrl,
                cancellationToken);

            return StatusCode(201);
        }

        /// <summary>
        /// Lista todos os mocks registrados.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Lista mocks", Description = "Retorna mocks cadastrados, opcionalmente filtrados por coleção", OperationId = "ListarMocks", Tags = new[] { "Mock" })]
        [SwaggerResponse(200, "Lista de mocks", typeof(IReadOnlyList<MockEntry>))]
        public async Task<ActionResult<IReadOnlyList<MockEntry>>> List(
            [FromQuery, SwaggerParameter("Filtrar por coleção")] string? collection,
            CancellationToken cancellationToken)
        {
            var list = await _mockService.ListAsync(collection, cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Remove um mock pelo método e path.
        /// </summary>
        [HttpDelete]
        [SwaggerOperation(Summary = "Remove mock", Description = "Remove o mock para o método e path informados", OperationId = "RemoverMock", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Mock removido")]
        [SwaggerResponse(400, "Parâmetros inválidos")]
        public async Task<IActionResult> Remove(
            [FromQuery, SwaggerParameter("Método HTTP", Required = true)] string method,
            [FromQuery, SwaggerParameter("Path do endpoint", Required = true)] string path,
            [FromQuery, SwaggerParameter("Coleção")] string? collection,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("method e path são obrigatórios.");
            }

            await _mockService.RemoveAsync(collection, method.Trim(), path, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Ativa ou desativa o bypass de um mock salvo.
        /// </summary>
        [HttpPatch("bypass")]
        [SwaggerOperation(Summary = "Atualiza bypass", Description = "Ativa ou desativa o bypass de um mock cadastrado", OperationId = "AtualizarBypass", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Bypass atualizado")]
        [SwaggerResponse(400, "Requisição inválida")]
        [SwaggerResponse(404, "Nenhum mock encontrado para o endpoint")]
        public async Task<IActionResult> UpdateBypass([FromBody] UpdateBypassRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Method) || string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Method e Path são obrigatórios.");
            }

            if (request.BypassEnabled && !IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl deve ser uma URL absoluta HTTP/HTTPS quando o bypass estiver ativo.");
            }

            var updated = await _mockService.UpdateBypassAsync(
                request.Collection,
                request.Method.Trim(),
                request.Path,
                request.BypassEnabled,
                request.BypassUrl,
                cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Ativa ou desativa um mock de coleção.
        /// </summary>
        [HttpPatch("enabled")]
        [SwaggerOperation(Summary = "Atualiza status do mock", Description = "Ativa ou desativa um mock em uma coleção. Inativo usa o bypass da coleção.", OperationId = "AtualizarMockEnabled", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Status atualizado")]
        [SwaggerResponse(400, "Requisição inválida")]
        [SwaggerResponse(404, "Nenhum mock encontrado para o endpoint")]
        public async Task<IActionResult> UpdateEnabled([FromBody] UpdateMockEnabledRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Collection) ||
                string.IsNullOrWhiteSpace(request.Method) ||
                string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Collection, Method e Path são obrigatórios.");
            }

            var updated = await _mockService.UpdateEnabledAsync(
                request.Collection.Trim(),
                request.Method.Trim(),
                request.Path,
                request.Enabled,
                cancellationToken);

            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Lista todas as coleções de mocks.
        /// </summary>
        [HttpGet("collections")]
        [SwaggerOperation(Summary = "Lista coleções", Description = "Retorna todas as coleções cadastradas", OperationId = "ListarColecoes", Tags = new[] { "Coleção" })]
        [SwaggerResponse(200, "Lista de coleções", typeof(IReadOnlyList<MockCollection>))]
        public async Task<ActionResult<IReadOnlyList<MockCollection>>> ListCollections(CancellationToken cancellationToken)
        {
            var list = await _collectionService.ListAsync(cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Cria uma nova coleção de mocks.
        /// </summary>
        [HttpPost("collections")]
        [SwaggerOperation(Summary = "Cria coleção", Description = "Registra uma coleção com URL de bypass", OperationId = "CriarColecao", Tags = new[] { "Coleção" })]
        [SwaggerResponse(201, "Coleção criada")]
        [SwaggerResponse(400, "Requisição inválida")]
        [SwaggerResponse(409, "Coleção já existe")]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest("Id é obrigatório.");
            }

            var normalizedId = MockStorePersistence.NormalizeCollectionId(request.Id);
            if (!MockStorePersistence.IsValidCollectionId(normalizedId))
            {
                return BadRequest("Id deve ser um slug válido (letras, números, _ e -) e não pode ser um nome reservado.");
            }

            if (!IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl deve ser uma URL absoluta HTTP/HTTPS.");
            }

            var created = await _collectionService.CreateAsync(normalizedId, request.BypassUrl, cancellationToken);
            return created ? StatusCode(201) : Conflict($"Coleção '{normalizedId}' já existe.");
        }

        /// <summary>
        /// Atualiza a URL de bypass de uma coleção.
        /// </summary>
        [HttpPatch("collections/{id}")]
        [SwaggerOperation(Summary = "Atualiza bypass da coleção", OperationId = "AtualizarBypassColecao", Tags = new[] { "Coleção" })]
        [SwaggerResponse(204, "Bypass da coleção atualizado")]
        [SwaggerResponse(400, "Requisição inválida")]
        [SwaggerResponse(404, "Coleção não encontrada")]
        public async Task<IActionResult> UpdateCollectionBypass(
            string id,
            [FromBody] UpdateCollectionRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl deve ser uma URL absoluta HTTP/HTTPS.");
            }

            var updated = await _collectionService.UpdateBypassAsync(id, request.BypassUrl, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Remove uma coleção e todos os mocks associados.
        /// </summary>
        [HttpDelete("collections/{id}")]
        [SwaggerOperation(Summary = "Remove coleção", Description = "Remove a coleção e exclui em cascata todos os mocks vinculados a ela.", OperationId = "RemoverColecao", Tags = new[] { "Coleção" })]
        [SwaggerResponse(204, "Coleção e mocks removidos")]
        [SwaggerResponse(404, "Coleção não encontrada")]
        public async Task<IActionResult> RemoveCollection(string id, CancellationToken cancellationToken)
        {
            var (success, error) = await _collectionService.RemoveAsync(id, cancellationToken);
            return success ? NoContent() : NotFound(error);
        }

        /// <summary>
        /// Serve respostas mockadas em /mock/{path} ou /mock/{colecao}/{endpoint}.
        /// </summary>
        [Route("{*path}")]
        [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
        [SwaggerOperation(
            Summary = "Servir mock",
            Description = "Retorna mock sem coleção ou mock de coleção quando o primeiro segmento da rota for uma coleção cadastrada. Ex.: GET /mock/ConsultarCliente ou GET /mock/parceiro/ConsultarCliente",
            OperationId = "ServirMock",
            Tags = new[] { "Mock" })]
        [SwaggerResponse(200, "Resposta mockada ou do bypass")]
        [SwaggerResponse(404, "Nenhum mock encontrado para o endpoint")]
        public async Task<IActionResult> Serve([FromRoute] string? path, CancellationToken cancellationToken)
        {
            var method = Request.Method;
            var decodedPath = Uri.UnescapeDataString(path ?? string.Empty);
            var routePath = decodedPath.TrimStart('/');
            var firstSegment = GetFirstPathSegment(routePath);

            if (!string.IsNullOrWhiteSpace(firstSegment))
            {
                var normalizedCollectionId = MockStorePersistence.NormalizeCollectionId(firstSegment);
                var mockCollection = await _collectionService.GetAsync(normalizedCollectionId, cancellationToken);
                if (mockCollection != null)
                {
                    var collectionPath = routePath[firstSegment.Length..].TrimStart('/');
                    return await ServeCollectionRequest(
                        normalizedCollectionId,
                        mockCollection,
                        method,
                        collectionPath,
                        cancellationToken);
                }
            }

            var normalizedPath = MockStorePersistence.NormalizePath(decodedPath);
            if (normalizedPath == "/")
            {
                return NotFound();
            }

            var entry = await _mockService.GetAsync(null, method, normalizedPath, cancellationToken);
            if (entry == null)
            {
                return NotFound();
            }

            if (entry.BypassEnabled)
            {
                if (!IsValidBypassUrl(entry.BypassUrl))
                {
                    return BadRequest("BypassUrl deve ser uma URL absoluta HTTP/HTTPS quando o bypass estiver ativo.");
                }

                await _bypassProxyService.ForwardAsync(HttpContext, entry.BypassUrl!, cancellationToken: cancellationToken);
                return new EmptyResult();
            }

            return await ServeMockEntry(entry, cancellationToken);
        }

        private async Task<IActionResult> ServeCollectionRequest(
            string normalizedCollectionId,
            MockCollection mockCollection,
            string method,
            string path,
            CancellationToken cancellationToken)
        {
            var normalizedPath = MockStorePersistence.NormalizePath(path);
            var entry = normalizedPath == "/"
                ? null
                : await _mockService.GetAsync(normalizedCollectionId, method, normalizedPath, cancellationToken);

            if (entry != null && entry.Enabled)
            {
                return await ServeMockEntry(entry, cancellationToken);
            }

            if (!IsValidBypassUrl(mockCollection.BypassUrl))
            {
                return BadRequest("BypassUrl da coleção deve ser uma URL absoluta HTTP/HTTPS.");
            }

            var stripPrefix = $"/mock/{normalizedCollectionId}";
            await _bypassProxyService.ForwardAsync(HttpContext, mockCollection.BypassUrl, stripPrefix, cancellationToken);
            return new EmptyResult();
        }

        private static string? GetFirstPathSegment(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var separatorIndex = path.IndexOf('/');
            return separatorIndex < 0 ? path : path[..separatorIndex];
        }

        private async Task<IActionResult> ServeMockEntry(MockEntry entry, CancellationToken cancellationToken)
        {
            if (entry.ResponseDelayMs > 0)
            {
                await Task.Delay(entry.ResponseDelayMs, cancellationToken);
            }

            if (entry.ResponseBody == null)
            {
                return new StatusCodeResult(entry.StatusCode);
            }

            var contentType = string.IsNullOrWhiteSpace(entry.ResponseContentType)
                ? "application/json"
                : entry.ResponseContentType.Trim();
            var content = FormatResponseBody(entry.ResponseBody);
            var result = Content(content, contentType, System.Text.Encoding.UTF8);
            result.StatusCode = entry.StatusCode;
            return result;
        }

        private static string FormatResponseBody(Newtonsoft.Json.Linq.JToken? responseBody)
        {
            if (responseBody == null)
            {
                return string.Empty;
            }

            if (responseBody.Type == Newtonsoft.Json.Linq.JTokenType.String)
            {
                return responseBody.ToObject<string>() ?? string.Empty;
            }

            return responseBody.ToString(Formatting.None);
        }

        private static bool IsValidBypassUrl(string? bypassUrl)
        {
            return Uri.TryCreate(bypassUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }
    }
}
