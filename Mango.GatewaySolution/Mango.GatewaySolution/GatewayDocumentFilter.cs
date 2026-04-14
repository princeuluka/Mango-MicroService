using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace Mango.GatewaySolution;

public class GatewayDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var ocelotConfig = LoadOcelotConfig();

        foreach (var route in ocelotConfig.Routes)
        {
            var pathTemplate = route.UpstreamPathTemplate;
            var methods = route.UpstreamHttpMethod ?? new List<string> { "GET" };

            if (!swaggerDoc.Paths.ContainsKey(pathTemplate))
            {
                swaggerDoc.Paths.Add(pathTemplate, new OpenApiPathItem());
            }

            var pathItem = swaggerDoc.Paths[pathTemplate];
            var serviceName = GetServiceName(route.DownstreamHostAndPorts?.FirstOrDefault()?.Port ?? 0);
            var requiresAuth = route.AuthenticationOptions?.AuthenticationProviderKey != null;

            foreach (var method in methods)
            {
                var operation = CreateOperation(method, serviceName, pathTemplate, requiresAuth);
                
                switch (method.ToUpper())
                {
                    case "GET":
                        pathItem.Operations[OperationType.Get] = operation;
                        break;
                    case "POST":
                        pathItem.Operations[OperationType.Post] = operation;
                        break;
                    case "PUT":
                        pathItem.Operations[OperationType.Put] = operation;
                        break;
                    case "DELETE":
                        pathItem.Operations[OperationType.Delete] = operation;
                        break;
                }
            }
        }

        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "Auth API", Description = "Authentication and user management" },
            new() { Name = "Coupon API", Description = "Coupon management operations" },
            new() { Name = "Product API", Description = "Product catalog operations" },
            new() { Name = "Cart API", Description = "Shopping cart operations" },
            new() { Name = "Order API", Description = "Order processing and management" }
        };
    }

    private OpenApiOperation CreateOperation(string method, string serviceName, string path, bool requiresAuth)
    {
        var operation = new OpenApiOperation
        {
            Tags = new List<OpenApiTag> { new() { Name = serviceName } },
            Summary = $"{method} {path}",
            Description = requiresAuth ? "Requires JWT authentication" : "Public endpoint",
            Responses = new OpenApiResponses
            {
                ["200"] = new OpenApiResponse { Description = "Success" },
                ["401"] = new OpenApiResponse { Description = "Unauthorized" },
                ["500"] = new OpenApiResponse { Description = "Internal Server Error" }
            }
        };

        if (requiresAuth)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
            {
                new()
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                }
            };
        }

        AddParametersForPath(operation, path, method);
        AddRequestBodyForMethod(operation, method, path);

        return operation;
    }

    private void AddParametersForPath(OpenApiOperation operation, string path, string method)
    {
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var segment in segments)
        {
            if (segment.StartsWith("{") && segment.EndsWith("}"))
            {
                var paramName = segment.Trim('{', '}');
                operation.Parameters.Add(new OpenApiParameter
                {
                    Name = paramName,
                    In = ParameterLocation.Path,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" }
                });
            }
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Contains("GetOrders"))
        {
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "userId",
                In = ParameterLocation.Query,
                Required = false,
                Schema = new OpenApiSchema { Type = "string" }
            });
        }
    }

    private void AddRequestBodyForMethod(OpenApiOperation operation, string method, string path)
    {
        if (method is "POST" or "PUT")
        {
            var schema = GetSchemaForPath(path);
            if (schema != null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        ["application/json"] = new OpenApiMediaType { Schema = schema }
                    }
                };
            }
        }
    }

    private OpenApiSchema? GetSchemaForPath(string path)
    {
        if (path.Contains("auth/login"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["username"] = new() { Type = "string" },
                    ["password"] = new() { Type = "string" }
                }
            };
        }

        if (path.Contains("auth/register"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["email"] = new() { Type = "string" },
                    ["name"] = new() { Type = "string" },
                    ["phoneNumber"] = new() { Type = "string" },
                    ["password"] = new() { Type = "string" },
                    ["role"] = new() { Type = "string" }
                }
            };
        }

        if (path.Contains("CouponAPI") && !path.Contains("GetByCode"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["couponId"] = new() { Type = "integer" },
                    ["couponCode"] = new() { Type = "string" },
                    ["discountAmount"] = new() { Type = "number" },
                    ["minAmount"] = new() { Type = "number" }
                }
            };
        }

        if (path.Contains("ProductAPI"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["productId"] = new() { Type = "integer" },
                    ["name"] = new() { Type = "string" },
                    ["price"] = new() { Type = "number" },
                    ["description"] = new() { Type = "string" },
                    ["categoryName"] = new() { Type = "string" },
                    ["imageUrl"] = new() { Type = "string" }
                }
            };
        }

        if (path.Contains("cart"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Description = "Cart or checkout data"
            };
        }

        if (path.Contains("order"))
        {
            return new OpenApiSchema
            {
                Type = "object",
                Description = "Order data"
            };
        }

        return null;
    }

    private string GetServiceName(int port)
    {
        return port switch
        {
            7001 => "Coupon API",
            7002 => "Auth API",
            7003 => "Cart API",
            7004 => "Product API",
            7005 => "Order API",
            _ => "API"
        };
    }

    private OcelotConfig LoadOcelotConfig()
    {
        var configPath = Path.Combine(AppContext.BaseDirectory, "ocelot.json");
        var json = File.ReadAllText(configPath);
        return JsonSerializer.Deserialize<OcelotConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new OcelotConfig();
    }
}

public class OcelotConfig
{
    public List<OcelotRoute> Routes { get; set; } = new();
    public object? GlobalConfiguration { get; set; }
}

public class OcelotRoute
{
    public string DownstreamPathTemplate { get; set; } = string.Empty;
    public string DownstreamScheme { get; set; } = string.Empty;
    public List<OcelotHostPort>? DownstreamHostAndPorts { get; set; }
    public string UpstreamPathTemplate { get; set; } = string.Empty;
    public List<string>? UpstreamHttpMethod { get; set; }
    public OcelotAuthOptions? AuthenticationOptions { get; set; }
}

public class OcelotHostPort
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
}

public class OcelotAuthOptions
{
    public string? AuthenticationProviderKey { get; set; }
    public List<string>? AllowedScopes { get; set; }
}
