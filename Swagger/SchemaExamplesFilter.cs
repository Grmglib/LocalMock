using LocalMock.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LocalMock.Swagger
{
    /// <summary>
    /// Adjusts Swagger examples for mock API scenarios.
    /// </summary>
    public class SchemaExamplesFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(CreateMockRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["collection"] = new OpenApiString("partner"),
                    ["method"] = new OpenApiString("GET"),
                    ["path"] = new OpenApiString("/customers"),
                    ["statusCode"] = new OpenApiInteger(200),
                    ["responseDelayMs"] = new OpenApiInteger(0),
                    ["enabled"] = new OpenApiBoolean(true),
                    ["bypassEnabled"] = new OpenApiBoolean(false),
                    ["bypassUrl"] = new OpenApiNull(),
                    ["responseBody"] = new OpenApiObject
                    {
                        ["status"] = new OpenApiString("OK"),
                        ["errorMessage"] = new OpenApiNull(),
                        ["data"] = new OpenApiObject
                        {
                            ["personType"] = new OpenApiString("individual"),
                            ["document"] = new OpenApiString("12345678901"),
                            ["name"] = new OpenApiString("Jane Doe")
                        }
                    }
                };
            }

            if (context.Type == typeof(CreateCollectionRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("partner"),
                    ["bypassUrl"] = new OpenApiString("https://api.partner.example.com")
                };
            }

            if (context.Type == typeof(UpdateCollectionRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["bypassUrl"] = new OpenApiString("https://api.partner.example.com")
                };
            }
        }
    }
}
