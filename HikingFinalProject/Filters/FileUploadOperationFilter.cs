using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace HikingFinalProject.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var fileParams = context.ApiDescription.ParameterDescriptions
                .Where(p => p.Type == typeof(IFormFile) ||
                            (p.Type.IsGenericType &&
                             p.Type.GetGenericTypeDefinition() == typeof(IEnumerable<>) &&
                             p.Type.GetGenericArguments()[0] == typeof(IFormFile)));

            if (!fileParams.Any()) return;

            operation.RequestBody = new OpenApiRequestBody
            {
                Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties = fileParams.ToDictionary(
                                p => p.Name,
                                p => new OpenApiSchema
                                {
                                    Type = "string",
                                    Format = "binary",
                                    Description = p.Name,
                                    Nullable = !p.IsRequired
                                }),
                            Required = new HashSet<string>(
                                fileParams.Where(p => p.IsRequired).Select(p => p.Name)
                            )
                        }
                    }
                }
            };
        }
    }
}

