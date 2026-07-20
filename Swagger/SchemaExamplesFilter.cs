using LocalMock.Models;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LocalMock.Swagger
{
    /// <summary>
    /// Ajusta exemplos do Swagger para cenários específicos da API de mock.
    /// </summary>
    public class SchemaExamplesFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(CreateMockRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["collection"] = new OpenApiString("parceiro"),
                    ["method"] = new OpenApiString("GET"),
                    ["path"] = new OpenApiString("/ConsultarCliente"),
                    ["statusCode"] = new OpenApiInteger(200),
                    ["responseDelayMs"] = new OpenApiInteger(0),
                    ["enabled"] = new OpenApiBoolean(true),
                    ["bypassEnabled"] = new OpenApiBoolean(false),
                    ["bypassUrl"] = new OpenApiNull(),
                    ["responseBody"] = new OpenApiObject
                    {
                        ["tipoResposta"] = new OpenApiString("OK"),
                        ["mensagemErro"] = new OpenApiNull(),
                        ["dados"] = new OpenApiObject
                        {
                            ["tipoPessoa"] = new OpenApiString("PF"),
                            ["cpfcnpj"] = new OpenApiString("12345678901"),
                            ["nome"] = new OpenApiString("João da Silva")
                        }
                    }
                };
            }

            if (context.Type == typeof(CreateCollectionRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["id"] = new OpenApiString("parceiro"),
                    ["bypassUrl"] = new OpenApiString("https://api.parceiro.exemplo.com")
                };
            }

            if (context.Type == typeof(UpdateCollectionRequest) && schema.Properties != null)
            {
                schema.Example = new OpenApiObject
                {
                    ["bypassUrl"] = new OpenApiString("https://api.parceiro.exemplo.com")
                };
            }
        }
    }
}
