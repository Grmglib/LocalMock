using LocalMock.Services;
using LocalMock.Swagger;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.OpenApi.Models;
using System.Reflection;

namespace LocalMock
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var isWindowsService = WindowsServiceHelpers.IsWindowsService();
            var contentRoot = isWindowsService
                ? AppContext.BaseDirectory
                : Directory.GetCurrentDirectory();

            if (isWindowsService)
            {
                Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            }

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions
            {
                Args = args,
                ContentRootPath = contentRoot
            });

            builder.Host.UseWindowsService();

            if (isWindowsService)
            {
                var dataDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                    "LocalMock");
                Directory.CreateDirectory(dataDirectory);

                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Mock:FilePath"] = Path.Combine(dataDirectory, "mocks.json")
                });
            }

            var port = builder.Configuration.GetValue<int>("LocalMock:Port", 5183);
            builder.WebHost.UseUrls($"http://localhost:{port}");

            builder.Services.Configure<UpdateOptions>(
                builder.Configuration.GetSection(UpdateOptions.SectionName));
            builder.Services.AddHttpClient(nameof(GitHubReleaseService), client =>
            {
                client.Timeout = TimeSpan.FromMinutes(5);
            });
            builder.Services.AddSingleton<IGitHubReleaseService, GitHubReleaseService>();
            builder.Services.AddSingleton<IAppUpdateService, AppUpdateService>();

            builder.Services.AddScoped<IMockService, MockService>();
            builder.Services.AddScoped<ICollectionService, CollectionService>();
            builder.Services.AddHttpForwarder();
            builder.Services.AddSingleton<IBypassProxyService, BypassProxyService>();

            builder.Services.AddControllers()
                .AddNewtonsoftJson(options =>
                {
                    options.SerializerSettings.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
                    options.SerializerSettings.DateFormatString = "yyyy-MM-dd";
                    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "API de Mock - Documentação",
                    Description = @"
## Documentação da API de Mock

Esta API centraliza o cadastro e a execução de respostas mockadas para apoiar testes de integração.

### Funcionalidades principais

- Criar ou atualizar mocks por método e path (por coleção)
- Gerenciar coleções com URL de bypass (`/mock/collections`)
- Servir mocks por coleção em `/mock/{colecao}/{endpoint}` (mock se existir, senão bypass)
- Rota `/mock/{path}` para mocks sem coleção

### Formato de Dados

- Todos os endpoints utilizam **JSON**
- O corpo de resposta do mock aceita **JSON livre**
"
                });

                options.EnableAnnotations();
                options.SchemaFilter<SchemaExamplesFilter>();

                var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
                if (File.Exists(xmlPath))
                {
                    options.IncludeXmlComments(xmlPath);
                }
            });

            builder.Services.AddSwaggerGenNewtonsoftSupport();

            var app = builder.Build();

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Equals("/ui", StringComparison.OrdinalIgnoreCase))
                {
                    context.Response.Redirect($"{context.Request.PathBase}/ui/");
                    return;
                }

                await next();
            });

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("v1/swagger.json", "API de Mock v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "Local Mock";
                options.DefaultModelsExpandDepth(2);
                options.DefaultModelExpandDepth(2);
                options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            });

            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/mock", StringComparison.OrdinalIgnoreCase) &&
                    !HttpMethods.IsGet(context.Request.Method) &&
                    !HttpMethods.IsHead(context.Request.Method))
                {
                    context.Request.EnableBuffering();
                }

                await next();
            });

            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }
}
