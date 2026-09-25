# LocalMock

LocalMock is a local HTTP mock server built with ASP.NET Core 8. It lets you define responses by HTTP method and path, organize them into collections, and forward requests to an upstream service when a collection has no active mock. It includes a browser-based manager and Swagger documentation.

## Requirements

- .NET 8 SDK to build and run from source
- Windows and PowerShell 5.1 or later for the optional Windows service scripts

## Quick start

```powershell
dotnet run --project LocalMock.csproj
```

The server listens on `http://localhost:5183` by default. Open the [mock manager](http://localhost:5183/ui/) or [Swagger UI](http://localhost:5183/swagger) in a browser.

Create and call a standalone mock:

```powershell
$mock = @{
    method = "GET"
    path = "/hello"
    statusCode = 200
    responseBody = @{ message = "Hello from LocalMock" }
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://localhost:5183/mock -ContentType "application/json" -Body $mock

Invoke-RestMethod http://localhost:5183/mock/hello
```

The GET request serves `{"message":"Hello from LocalMock"}`. Posting the same method, path, and collection again updates the existing mock.

## Collections and bypass

A collection groups mocks under `/mock/{collectionId}/...` and requires an absolute HTTP or HTTPS bypass URL. When a request has no matching enabled mock, LocalMock forwards it to that URL, preserving the remaining path and query string. For example, with bypass URL `https://api.example.com`, a request to `/mock/demo/users?id=1` is forwarded to `https://api.example.com/users?id=1`.

```powershell
$collection = @{ id = "demo"; bypassUrl = "https://api.example.com" } | ConvertTo-Json
Invoke-RestMethod -Method Post -Uri http://localhost:5183/mock/collections -ContentType "application/json" -Body $collection

$mock = @{
    collection = "demo"
    method = "GET"
    path = "/users"
    responseBody = @(@{ id = 1; name = "Ada" })
} | ConvertTo-Json -Depth 5
Invoke-RestMethod -Method Post -Uri http://localhost:5183/mock -ContentType "application/json" -Body $mock

Invoke-RestMethod http://localhost:5183/mock/demo/users
```

Collection IDs may contain letters, digits, `_`, and `-`; `collections`, `bypass`, and `enabled` are reserved. A standalone mock can also be configured with its own `bypassEnabled` and `bypassUrl`. For collection mocks, the `enabled` flag controls whether the saved response is served or the collection bypass is used.

## Mock options

`POST /mock` accepts these JSON fields:

| Field | Meaning |
| --- | --- |
| `collection` | Optional collection ID; omit for `/mock/{path}`. The collection must already exist. |
| `method`, `path` | HTTP method and endpoint path to match. |
| `statusCode` | Response status code; defaults to `200`. |
| `responseBody` | JSON value to return. A JSON string is emitted as plain text. |
| `responseContentType` | Response content type; defaults to `application/json`. |
| `responseDelayMs` | Delay before the mock response, in milliseconds; defaults to `0`. |
| `enabled` | Whether a collection mock is active; defaults to `true`. |
| `bypassEnabled`, `bypassUrl` | Per-mock forwarding settings for standalone mocks. |

Requests are matched by method, path, and collection. Query strings are not part of mock matching. The serving route supports `GET`, `POST`, `PUT`, `PATCH`, and `DELETE`.

## API reference

| Method | Route | Purpose |
| --- | --- | --- |
| `POST` | `/mock` | Create or update a mock. |
| `GET` | `/mock?collection={id}` | List mocks; the filter is optional. |
| `DELETE` | `/mock?method={method}&path={path}&collection={id}` | Remove a mock; `collection` is optional. |
| `PATCH` | `/mock/enabled` | Enable or disable a collection mock. |
| `PATCH` | `/mock/bypass` | Update a mock's bypass settings. |
| `GET`, `POST`, `PUT`, `PATCH`, `DELETE` | `/mock/{path}` or `/mock/{collectionId}/{path}` | Serve a mock or forward the request. |
| `GET`, `POST` | `/mock/collections` | List or create collections. |
| `PATCH`, `DELETE` | `/mock/collections/{id}` | Update a collection's bypass URL or delete the collection and its mocks. |
| `GET` | `/api/version` | Check the installed version and latest GitHub release. |
| `POST` | `/api/update` | Start an update from the configured GitHub release asset. |

See `/swagger` for request schemas and response details.

## Configuration and storage

Settings are in `appsettings.json` and can be overridden with standard ASP.NET Core configuration sources. The main settings are `LocalMock:Port` (default `5183`), `Mock:FilePath` (default `mocks.json` relative to the content root), and `LocalMock:Updates` (GitHub owner, repository, release asset name, check interval, and optional token).

Mocks and collections are saved together in the configured JSON file. When installed as a Windows service, the app stores them at `%ProgramData%\LocalMock\mocks.json` regardless of the `Mock:FilePath` setting. The server binds to `localhost`, so it is intended for access from the same machine.

## Windows service

Run these scripts in an elevated PowerShell session:

```powershell
.\scripts\publish.ps1
.\scripts\install-service.ps1
```

The publish script creates `artifacts\publish` and `artifacts\LocalMock-win-x64.zip`. The install script copies the published files to `C:\Program Files\LocalMock`, registers an automatically starting `LocalMock` service, and keeps its data under `%ProgramData%\LocalMock`.

To uninstall the service, run `.\scripts\uninstall-service.ps1`. It preserves the data directory unless you pass `-RemoveData`.

The UI can check GitHub Releases for updates. The update endpoint expects a release asset named `LocalMock-win-x64.zip` by default and uses the bundled `apply-update.ps1` script to replace the installed files and restart the service.
