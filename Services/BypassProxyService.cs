using Microsoft.AspNetCore.Http;
using System.Diagnostics;
using System.Net;
using Yarp.ReverseProxy.Forwarder;

namespace LocalMock.Services;

public class BypassProxyService : IBypassProxyService, IDisposable
{
    private readonly ForwarderRequestConfig _requestConfig = new()
    {
        ActivityTimeout = TimeSpan.FromSeconds(100)
    };

    private readonly HttpMessageInvoker _httpClient = new(new SocketsHttpHandler
    {
        UseProxy = false,
        AllowAutoRedirect = false,
        AutomaticDecompression = DecompressionMethods.None,
        UseCookies = false,
        EnableMultipleHttp2Connections = true,
        ActivityHeadersPropagator = new ReverseProxyPropagator(DistributedContextPropagator.Current),
        ConnectTimeout = TimeSpan.FromSeconds(15)
    });

    private readonly IHttpForwarder _forwarder;
    private readonly ILogger<BypassProxyService> _logger;

    public BypassProxyService(IHttpForwarder forwarder, ILogger<BypassProxyService> logger)
    {
        _forwarder = forwarder;
        _logger = logger;
    }

    public async Task<bool> ForwardAsync(
        HttpContext context,
        string bypassUrl,
        string? pathPrefixToStrip = null,
        CancellationToken cancellationToken = default)
    {
        var destinationUri = BuildTargetUri(bypassUrl, context.Request.Path, context.Request.QueryString, pathPrefixToStrip);

        ResetRequestBody(context.Request);
        LogBypassRequest(context.Request, destinationUri);

        var error = await _forwarder.SendAsync(
            context,
            GetDestinationPrefix(destinationUri),
            _httpClient,
            _requestConfig,
            new BypassHttpTransformer(destinationUri),
            cancellationToken);

        if (error == ForwarderError.None)
        {
            return true;
        }

        var errorFeature = context.Features.Get<IForwarderErrorFeature>();
        _logger.LogWarning(
            errorFeature?.Exception,
            "Failed to forward bypass to {DestinationUri}. ForwarderError={ForwarderError}",
            destinationUri,
            error);

        if (!context.Response.HasStarted)
        {
            context.Response.StatusCode = StatusCodes.Status502BadGateway;
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Unable to forward the request to the bypass URL.",
                detail = "Check that the URL, port, and HTTP/HTTPS protocol are correct."
            }, cancellationToken);
        }

        return false;
    }

    private static void ResetRequestBody(HttpRequest request)
    {
        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method))
        {
            return;
        }

        request.EnableBuffering();
        if (request.Body.CanSeek)
        {
            request.Body.Position = 0;
        }
    }

    private static Uri BuildTargetUri(string bypassUrl, PathString requestPath, QueryString queryString, string? pathPrefixToStrip = null)
    {
        var builder = new UriBuilder(bypassUrl);
        var bypassPath = builder.Path.TrimEnd('/');
        var mockPath = GetMockPath(requestPath, pathPrefixToStrip).TrimStart('/');
        if (!string.IsNullOrWhiteSpace(mockPath) && !bypassPath.EndsWith("/" + mockPath, StringComparison.OrdinalIgnoreCase))
        {
            builder.Path = string.IsNullOrWhiteSpace(bypassPath) || bypassPath == "/"
                ? mockPath
                : bypassPath + "/" + mockPath;
        }

        var incomingQuery = queryString.Value?.TrimStart('?');
        if (!string.IsNullOrWhiteSpace(incomingQuery))
        {
            var existingQuery = builder.Query.TrimStart('?');
            builder.Query = string.IsNullOrWhiteSpace(existingQuery)
                ? incomingQuery
                : existingQuery + "&" + incomingQuery;
        }

        return builder.Uri;
    }

    private static string FormatHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
    {
        return string.Join("; ", headers.Select(header =>
            header.Key + "=" + string.Join(",", header.Value.Select(value => RedactHeaderValue(header.Key, value)))));
    }

    private void LogBypassRequest(HttpRequest request, Uri destinationUri)
    {
        _logger.LogInformation(
            "Bypass request | Incoming: {IncomingMethod} {IncomingPath}{IncomingQuery} ContentType={IncomingContentType} ContentLength={IncomingContentLength} Headers={IncomingHeaders} | Destination: {DestinationUri}",
            request.Method,
            request.Path,
            request.QueryString,
            request.ContentType ?? "<no content-type>",
            request.ContentLength?.ToString() ?? "<no content-length>",
            FormatHeaders(request.Headers.Select(header => new KeyValuePair<string, IEnumerable<string>>(
                header.Key,
                header.Value.Select(value => value ?? string.Empty)))),
            destinationUri);
    }

    private static string GetMockPath(PathString requestPath, string? pathPrefixToStrip = null)
    {
        var path = requestPath.Value ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(pathPrefixToStrip))
        {
            var prefix = pathPrefixToStrip.StartsWith("/", StringComparison.Ordinal) ? pathPrefixToStrip : "/" + pathPrefixToStrip;
            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return path[prefix.Length..];
            }
        }

        const string mockPrefix = "/mock/";
        if (path.StartsWith(mockPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[mockPrefix.Length..];
        }

        return path.TrimStart('/');
    }

    private static string GetDestinationPrefix(Uri destinationUri)
    {
        return destinationUri.GetLeftPart(UriPartial.Authority);
    }

    private static string RedactHeaderValue(string headerName, string value)
    {
        return IsSensitiveHeader(headerName) ? "<redacted>" : value;
    }

    private static bool IsSensitiveHeader(string headerName)
    {
        return headerName.Equals("Authorization", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
            headerName.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("token", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("secret", StringComparison.OrdinalIgnoreCase) ||
            headerName.Contains("password", StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private sealed class BypassHttpTransformer : HttpTransformer
    {
        private readonly Uri _destinationUri;

        public BypassHttpTransformer(Uri destinationUri)
        {
            _destinationUri = destinationUri;
        }

        public override async ValueTask TransformRequestAsync(
            HttpContext httpContext,
            HttpRequestMessage proxyRequest,
            string destinationPrefix,
            CancellationToken cancellationToken)
        {
            await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix, cancellationToken);

            proxyRequest.RequestUri = _destinationUri;
            proxyRequest.Headers.Host = null;
        }
    }
}
