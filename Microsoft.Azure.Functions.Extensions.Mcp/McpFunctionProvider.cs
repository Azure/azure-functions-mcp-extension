using System.Collections.Immutable;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Script.Description;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

public class McpFunctionProvider : IFunctionProvider
{
    public Task<ImmutableArray<FunctionMetadata>> GetFunctionMetadataAsync()
    {
        ImmutableArray<FunctionMetadata> functions =
        [
            GetFunctionMetadata(nameof(McpFunctions.HandleSseRequest), "sse", HttpMethods.Get),
            GetFunctionMetadata(nameof(McpFunctions.HandleMessageRequest), "message", HttpMethods.Post),
        ];

        return Task.FromResult(functions);
    }

    public ImmutableDictionary<string, ImmutableArray<string>> FunctionErrors => ImmutableDictionary<string, ImmutableArray<string>>.Empty;

    private static FunctionMetadata GetFunctionMetadata(string functionName, string route, params string[] methods)
    {
        var functionsType = typeof(McpFunctions);
        var functionMetadata = new FunctionMetadata()
        {
            Name = functionName,
            FunctionDirectory = null,
            ScriptFile = $"assembly:{functionsType.Assembly.FullName}",
            EntryPoint = $"{functionsType.Assembly.GetName().Name}.{functionsType.Name}.{functionName}",
            Language = "DotNetAssembly",
        };

        functionMetadata.Bindings.Add(CreateBinding(route));

        return functionMetadata;
    }

    private static BindingMetadata CreateBinding(string route, params string[] methods)
    {
        var binding = new HttpBindingMetadata()
        {
            Methods = methods,
            Route = route,
            AuthLevel = AuthorizationLevel.Anonymous,
        };

        return BindingMetadata.Create(JObject.FromObject(binding));
    }
}