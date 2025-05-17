[![Build Status](https://azfunc.visualstudio.com/public/_apis/build/status%2Fazure%2Fazure-functions-mcp-extension%2Fmcp-extension.public?repoName=Azure%2Fazure-functions-mcp-extension&branchName=main)](https://azfunc.visualstudio.com/public/_build/latest?definitionId=1375&repoName=Azure%2Fazure-functions-mcp-extension&branchName=main)

# Azure Functions MCP extension (Preview)

This repository contains an extension of [Azure Functions](https://functions.azure.com) to enable support for [Model Context Protocol (MCP)](https://modelcontextprotocol.io/). Using this extension, an Azure Functions application can act as a scalable remote MCP server. The extension includes an MCP tool trigger to help you define tools that can be used by clients such as AI agents to perform defined tasks during their operation.

In addition to the host extension which supports all stacks, this repository also includes an integration for Functions projects using .NET on the isolated worker model.

## Instructions

> [!IMPORTANT]
> The Azure Functions MCP extension is currently in preview. You can expect changes to the trigger and binding APIs prior to the extension becoming generally available.
> You should avoid using preview extensions in production apps.

To get started with the extension, please see the following samples:

| Language (Stack) | Repo Location |
|------------------|---------------|
| C# (.NET) | [remote-mcp-functions-dotnet](https://github.com/Azure-Samples/remote-mcp-functions-dotnet) |
| Python | [remote-mcp-functions-python](https://github.com/Azure-Samples/remote-mcp-functions-python) |
| TypeScript (Node.js) | [remote-mcp-functions-typescript](https://github.com/Azure-Samples/remote-mcp-functions-typescript) |
| Java | [remote-mcp-functions-java](https://github.com/Azure-Samples/remote-mcp-functions-java) |

Additional information can also be found in the [Azure Functions documentation](https://aka.ms/functions-mcp).

### Configuration

You can configure the extension behavior using the `host.json` file. The following is an example of the configurable settings:

``` json
{
  "version": "2.0",
  "extensions": {
    "mcp": {
      "instructions": "Some test instructions on how to use the server",
      "serverName": "TestServer",
      "serverVersion": "2.0.0"
    }
  }
}
```

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
