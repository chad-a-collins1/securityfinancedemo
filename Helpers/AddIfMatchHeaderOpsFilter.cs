using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
public class AddIfMatchHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var method = context.ApiDescription.HttpMethod;

        if (method is "PUT" or "PATCH" or "DELETE")
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "If-Match",
                In = ParameterLocation.Header,
                Required = true,
                Schema = new OpenApiSchema { Type = "string" },
                Description = "Required ETag header for concurrency control"
            });
        }
    }
}