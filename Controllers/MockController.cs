using LocalMock.Models;
using LocalMock.Services;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Swashbuckle.AspNetCore.Annotations;

namespace LocalMock.Controllers
{
    /// <summary>
    /// Mock API endpoints for creating, listing, and serving mocked responses.
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
        /// Creates or updates a mock for an endpoint.
        /// </summary>
        [HttpPost]
        [SwaggerOperation(Summary = "Create mock", Description = "Registers a mock for an HTTP method and path", OperationId = "CreateMock", Tags = new[] { "Mock" })]
        [SwaggerResponse(201, "Mock created successfully")]
        [SwaggerResponse(400, "Invalid request")]
        public async Task<IActionResult> Create([FromBody] CreateMockRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Method) || string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Method and Path are required.");
            }

            if (request.StatusCode < 100 || request.StatusCode > 599)
            {
                return BadRequest("StatusCode must be between 100 and 599.");
            }

            if (request.BypassEnabled && !IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl must be an absolute HTTP/HTTPS URL when bypass is enabled.");
            }

            if (request.ResponseDelayMs < 0)
            {
                return BadRequest("ResponseDelayMs must be greater than or equal to zero.");
            }

            var collection = MockStorePersistence.NormalizeCollectionReference(request.Collection);
            if (!string.IsNullOrWhiteSpace(collection))
            {
                if (!MockStorePersistence.IsValidCollectionId(collection))
                {
                    return BadRequest("Collection must be a valid slug (letters, digits, _ and -).");
                }

                if (!await _collectionService.ExistsAsync(collection, cancellationToken))
                {
                    return BadRequest($"Collection '{collection}' was not found.");
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
        /// Lists all registered mocks.
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "List mocks", Description = "Returns registered mocks, optionally filtered by collection", OperationId = "ListMocks", Tags = new[] { "Mock" })]
        [SwaggerResponse(200, "Mock list", typeof(IReadOnlyList<MockEntry>))]
        public async Task<ActionResult<IReadOnlyList<MockEntry>>> List(
            [FromQuery, SwaggerParameter("Filter by collection")] string? collection,
            CancellationToken cancellationToken)
        {
            var list = await _mockService.ListAsync(collection, cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Removes a mock by method and path.
        /// </summary>
        [HttpDelete]
        [SwaggerOperation(Summary = "Remove mock", Description = "Removes the mock for the given method and path", OperationId = "RemoveMock", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Mock removed")]
        [SwaggerResponse(400, "Invalid parameters")]
        public async Task<IActionResult> Remove(
            [FromQuery, SwaggerParameter("HTTP method", Required = true)] string method,
            [FromQuery, SwaggerParameter("Endpoint path", Required = true)] string path,
            [FromQuery, SwaggerParameter("Collection")] string? collection,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(method) || string.IsNullOrWhiteSpace(path))
            {
                return BadRequest("method and path are required.");
            }

            await _mockService.RemoveAsync(collection, method.Trim(), path, cancellationToken);
            return NoContent();
        }

        /// <summary>
        /// Enables or disables bypass for a saved mock.
        /// </summary>
        [HttpPatch("bypass")]
        [SwaggerOperation(Summary = "Update bypass", Description = "Enables or disables bypass for a registered mock", OperationId = "UpdateBypass", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Bypass updated")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(404, "No mock found for the endpoint")]
        public async Task<IActionResult> UpdateBypass([FromBody] UpdateBypassRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Method) || string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Method and Path are required.");
            }

            if (request.BypassEnabled && !IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl must be an absolute HTTP/HTTPS URL when bypass is enabled.");
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
        /// Enables or disables a collection mock.
        /// </summary>
        [HttpPatch("enabled")]
        [SwaggerOperation(Summary = "Update mock status", Description = "Enables or disables a mock in a collection. When disabled, the collection bypass is used.", OperationId = "UpdateMockEnabled", Tags = new[] { "Mock" })]
        [SwaggerResponse(204, "Status updated")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(404, "No mock found for the endpoint")]
        public async Task<IActionResult> UpdateEnabled([FromBody] UpdateMockEnabledRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Collection) ||
                string.IsNullOrWhiteSpace(request.Method) ||
                string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest("Collection, Method, and Path are required.");
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
        /// Lists all mock collections.
        /// </summary>
        [HttpGet("collections")]
        [SwaggerOperation(Summary = "List collections", Description = "Returns all registered collections", OperationId = "ListCollections", Tags = new[] { "Collection" })]
        [SwaggerResponse(200, "Collection list", typeof(IReadOnlyList<MockCollection>))]
        public async Task<ActionResult<IReadOnlyList<MockCollection>>> ListCollections(CancellationToken cancellationToken)
        {
            var list = await _collectionService.ListAsync(cancellationToken);
            return Ok(list);
        }

        /// <summary>
        /// Creates a new mock collection.
        /// </summary>
        [HttpPost("collections")]
        [SwaggerOperation(Summary = "Create collection", Description = "Registers a collection with a bypass URL", OperationId = "CreateCollection", Tags = new[] { "Collection" })]
        [SwaggerResponse(201, "Collection created")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(409, "Collection already exists")]
        public async Task<IActionResult> CreateCollection([FromBody] CreateCollectionRequest request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
            {
                return BadRequest("Id is required.");
            }

            var normalizedId = MockStorePersistence.NormalizeCollectionId(request.Id);
            if (!MockStorePersistence.IsValidCollectionId(normalizedId))
            {
                return BadRequest("Id must be a valid slug (letters, digits, _ and -) and cannot be a reserved name.");
            }

            if (!IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl must be an absolute HTTP/HTTPS URL.");
            }

            var created = await _collectionService.CreateAsync(normalizedId, request.BypassUrl, cancellationToken);
            return created ? StatusCode(201) : Conflict($"Collection '{normalizedId}' already exists.");
        }

        /// <summary>
        /// Updates the bypass URL of a collection.
        /// </summary>
        [HttpPatch("collections/{id}")]
        [SwaggerOperation(Summary = "Update collection bypass", OperationId = "UpdateCollectionBypass", Tags = new[] { "Collection" })]
        [SwaggerResponse(204, "Collection bypass updated")]
        [SwaggerResponse(400, "Invalid request")]
        [SwaggerResponse(404, "Collection not found")]
        public async Task<IActionResult> UpdateCollectionBypass(
            string id,
            [FromBody] UpdateCollectionRequest request,
            CancellationToken cancellationToken)
        {
            if (!IsValidBypassUrl(request.BypassUrl))
            {
                return BadRequest("BypassUrl must be an absolute HTTP/HTTPS URL.");
            }

            var updated = await _collectionService.UpdateBypassAsync(id, request.BypassUrl, cancellationToken);
            return updated ? NoContent() : NotFound();
        }

        /// <summary>
        /// Removes a collection and all associated mocks.
        /// </summary>
        [HttpDelete("collections/{id}")]
        [SwaggerOperation(Summary = "Remove collection", Description = "Removes the collection and cascadingly deletes all mocks linked to it.", OperationId = "RemoveCollection", Tags = new[] { "Collection" })]
        [SwaggerResponse(204, "Collection and mocks removed")]
        [SwaggerResponse(404, "Collection not found")]
        public async Task<IActionResult> RemoveCollection(string id, CancellationToken cancellationToken)
        {
            var (success, error) = await _collectionService.RemoveAsync(id, cancellationToken);
            return success ? NoContent() : NotFound(error);
        }

        /// <summary>
        /// Serves mocked responses at /mock/{path} or /mock/{collection}/{endpoint}.
        /// </summary>
        [Route("{*path}")]
        [AcceptVerbs("GET", "POST", "PUT", "DELETE", "PATCH")]
        [SwaggerOperation(
            Summary = "Serve mock",
            Description = "Returns a standalone mock or a collection mock when the first route segment is a registered collection. Example: GET /mock/customers or GET /mock/partner/customers",
            OperationId = "ServeMock",
            Tags = new[] { "Mock" })]
        [SwaggerResponse(200, "Mocked or bypass response")]
        [SwaggerResponse(404, "No mock found for the endpoint")]
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
                    return BadRequest("BypassUrl must be an absolute HTTP/HTTPS URL when bypass is enabled.");
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
                return BadRequest("Collection BypassUrl must be an absolute HTTP/HTTPS URL.");
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
