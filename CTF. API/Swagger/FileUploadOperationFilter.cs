using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CTF.Api.Swagger;

/// <summary>
/// Fixes Swashbuckle 6 error when an endpoint uses [FromForm] IFormFile.
/// Replaces the parameter with a proper multipart/form-data requestBody.
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParams = context.MethodInfo
            .GetParameters()
            .Where(p => p.ParameterType == typeof(IFormFile))
            .Select(p => p.Name!)
            .ToList();

        if (fileParams.Count == 0) return;

        // Build a multipart/form-data requestBody with binary fields for each IFormFile
        var properties = fileParams.ToDictionary(
            name => name,
            _ => new OpenApiSchema { Type = "string", Format = "binary" }
        );

        operation.RequestBody = new OpenApiRequestBody
        {
            Required = true,
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type       = "object",
                        Properties = properties,
                        Required   = fileParams.ToHashSet()
                    }
                }
            }
        };

        // Remove IFormFile parameters from the query/header params list
        if (operation.Parameters is not null)
        {
            operation.Parameters = operation.Parameters
                .Where(p => !fileParams.Contains(p.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
