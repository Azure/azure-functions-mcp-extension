# Azure Functions MCP extension

Extension to enable MCP support to Azure Functions.

## Instructions

To get started with the extension, please see the following samples:

| Language (Stack) | Repo Location |
|------------------|---------------|
| C# (.NET) | [remote-mcp-functions-dotnet](https://github.com/Azure-Samples/remote-mcp-functions-dotnet) |
| Python | [remote-mcp-functions-python](https://github.com/Azure-Samples/remote-mcp-functions-python) |
| TypeScript (Node.js) | [remote-mcp-functions-typescript](https://github.com/Azure-Samples/remote-mcp-functions-typescript) |

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
