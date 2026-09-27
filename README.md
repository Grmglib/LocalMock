# LocalMock

LocalMock is a local HTTP mock server built with ASP.NET Core 8. It lets you define responses by HTTP method and path, organize them into collections, and forward requests to an upstream service when a collection has no active mock. It includes a browser-based manager and Swagger documentation.

## Requirements

- Windows x64 with the .NET 8 Desktop Runtime and ASP.NET Core Runtime to run the published app
- .NET 8 SDK to build and run from source
- Node.js 20.19+ (or 22.12+) and pnpm to build the Vue manager
- PowerShell 5.1 or later for automatic updates

## Quick start

```powershell
.\scripts\build-ui.ps1
dotnet run --project LocalMock.csproj
```

The server listens on `http://localhost:5183` by default and opens the Vue manager at [http://localhost:5183/ui/](http://localhost:5183/ui/). The .NET build serves the static Vue files directly and does not build the frontend. Closing the browser tab leaves LocalMock running; use **Exit** in the Windows tray to stop it. The [Swagger UI](http://localhost:5183/swagger) remains available.

Vue is the only manager. Run `.\scripts\build-ui.ps1` to generate it at `/ui/`, or run `pnpm --dir frontend run dev` and open the Vite URL for development. Its dev server proxies `/mock` and `/api` requests to LocalMock. The publish script builds Vue before packaging; the .NET build itself still does not build Vue.

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
| `GET` | `/api/version/current` | Read the installed version without contacting GitHub. |
| `POST` | `/api/update` | Start an update from the configured GitHub release asset. |

See `/swagger` for request schemas and response details.

## Configuration and storage

Settings are in `appsettings.json` and can be overridden with standard ASP.NET Core configuration sources. The main settings are `LocalMock:Port` (default `5183`), `Mock:FilePath` (optional override), and `LocalMock:Updates` (GitHub owner, repository, release asset name, and optional token).

Mocks and collections are saved together in `%LocalAppData%\LocalMock\mocks.json` by default. An explicit `Mock:FilePath` overrides this location; relative paths are resolved from the application folder. The server binds to `localhost`, so it is intended for access from the same machine.

## Portable Windows app

Download `LocalMock-win-x64.zip`, extract it into a folder you can write to, and run `LocalMock.exe`. Keep the files from the ZIP together. No administrator rights or service registration are needed. Running the EXE again opens the existing instance's manager page.

To build the same ZIP from source, run `.\scripts\publish.ps1`. It publishes the backend and packages the static files currently present under `wwwroot` into `artifacts\publish` and `artifacts\LocalMock-win-x64.zip` using a framework-dependent publication. It builds Vue and packages the Vue manager. Run `.\scripts\smoke-test.ps1` to check the published manager and a mock request.

The UI checks GitHub Releases when opened and when you click the check button. If a newer release contains `LocalMock-win-x64.zip`, an **Update** button appears beside it. Applying the update replaces the extracted application files, then restarts LocalMock in the tray. The open page reloads when the new version is ready. Update logs are written to `%TEMP%\LocalMock-update.log`.
