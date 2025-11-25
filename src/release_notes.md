## What's Changed

<!-- Please add your release notes in the following format:
- My change description (#PR/#issue)
-->

### Microsoft.Azure.Functions.Extensions 1.1.0

- Added support for enum property types (#131)
- Updated MCP SDK reference to 0.4.0-preview.3 (#140)
- Added support for additional MCP tool result content types, including: audio, image, resource, and resource link. (#130)
- Updated project dependencies (#144)
    - Upgraded Azure Functions Worker and related packages to latest versions
    - Updated package versions for Azure Storage, WebJobs, and other dependencies in Extensions.Mcp.csproj to ensure security and stability.

### Microsoft.Azure.Functions.Worker.Extensions.Mcp <version>

- Fix argument type conversion logic in MCP input binding; now correctly handle Guid and DateTime types (#126)
- Added support for collection/array property types in fluent tool definition APIs (#128)
- Added support for enum property types (#131)
- Updating McpInputConversionHelper to handle string target conversions (#145)
