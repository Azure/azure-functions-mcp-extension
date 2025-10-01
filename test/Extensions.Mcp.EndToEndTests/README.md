# Azure Functions MCP Extension - End-to-End Tests

This project contains comprehensive end-to-end tests for the Azure Functions Model Context Protocol (MCP) Extension. These tests validate the complete functionality of both InProc (.NET 8) and Isolated Worker (.NET) Azure Functions apps acting as MCP servers.

## Overview

The E2E tests verify:
- **Protocol Compliance**: Proper MCP protocol implementation
- **Tool Invocation**: Function tools can be called via MCP
- **Session Management**: Client sessions are properly managed
- **Transport Modes**: SSE, StreamableHttp, and AutoDetect transports
- **Server Types**: Both InProc and Isolated Worker hosting models
- **Configuration**: Different MCP server configurations and options

## Project Structure
test/Extensions.Mcp.EndToEndTests/
├── Abstractions/                    # Core test infrastructure
│   ├── CoreToolsProjectBase.cs     # Azure Functions Core Tools management
│   ├── TestUtility.cs               # Common test utilities
│   ├── JobObjectRegistry.cs         # Process cleanup utilities
│   └── EndToEndTestProject.cs       # Test project configuration
├── Fixtures/                        # Test fixtures and setup
│   ├── McpEndToEndFixtureBase.cs    # Base fixture for E2E tests
│   ├── McpEndToEndProjectFixtures.cs # Concrete fixtures for different server types
│   └── McpEndToEndProjects.cs       # Project configuration classes
├── ProtocolTests/                   # MCP protocol compliance tests
│   ├── Base*.cs                     # Base test classes
│   ├── InProc/                      # InProc server-specific tests
│   └── Default/                     # Isolated Worker server-specific tests
└── InvocationTests/                 # Direct HTTP invocation tests
    ├── InProc/                      # InProc server invocation tests
    └── Default/                     # Isolated Worker server invocation tests
## Prerequisites

### Required Software

Before running the E2E tests, ensure you have the following installed:

1. **.NET SDK 8.0 or later**
2. **Azure Functions Core Tools**
3. **Azurite**

### Test Projects

The E2E tests use two test Function Apps:
- **TestApp** - InProc (.NET 8) Functions app
- **TestAppIsolated** - Isolated Worker Functions app

These must be built before running tests:
```bash
dotnet build test/TestApp
dotnet build test/TestAppIsolated
```

## Running Tests

### Full Test Suite

Run all E2E tests:dotnet test test/Extensions.Mcp.EndToEndTests
### Test Categories

**Protocol Tests** (test MCP protocol compliance):dotnet test test/Extensions.Mcp.EndToEndTests --filter "Category=Protocol"
# Or by namespace
dotnet test test/Extensions.Mcp.EndToEndTests --filter "FullyQualifiedName~ProtocolTests"
**Invocation Tests** (test direct HTTP tool calls):dotnet test test/Extensions.Mcp.EndToEndTests --filter "FullyQualifiedName~InvocationTests"
### Server-Specific Tests

**InProc Server Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "InProc"
**Isolated Worker Server Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "Default"
### Transport-Specific Tests

Tests run against multiple transport modes. You can filter by specific transport:
# SSE transport tests
dotnet test test/Extensions.Mcp.EndToEndTests --filter "TestCategory=SSE"

# StreamableHttp transport tests  
dotnet test test/Extensions.Mcp.EndToEndTests --filter "TestCategory=StreamableHttp"
### Specific Test Classes

**Session Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "*SessionTests"
**Tool Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "*ToolTests"
**Initialization Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "*InitializationTests"
## Configuration Tests

The E2E tests include tests for various MCP configurations:

**Absolute URI Endpoint Tests:**dotnet test test/Extensions.Mcp.EndToEndTests --filter "*AbsoluteUri*"
These tests validate the `UseAbsoluteUriForEndpoint` configuration option.

## Test Architecture

### Fixtures

- **McpEndToEndFixtureBase**: Base class that manages Azure Functions Core Tools lifecycle
- **InProcProjectFixture**: Fixture for testing InProc (.NET 8) Functions
- **DefaultProjectFixture**: Fixture for testing Isolated Worker Functions

### Base Test Classes

- **BaseInitializationTests**: Tests server initialization and capabilities
- **BaseToolTests**: Tests tool listing and discovery
- **BasePingTests**: Tests client-server ping functionality
- **BaseSessionTests**: Tests session management
- **ServerToolInvocationBase**: Tests direct HTTP tool invocation

### Transport Modes

Tests run against three transport modes:
- **SSE (Server-Sent Events)**: Long-lived connection with server-sent events
- **StreamableHttp**: Request-response HTTP calls
- **AutoDetect**: Client automatically chooses the best transport

## Debugging

### Verbose Output

Run tests with verbose output to see detailed logs:dotnet test test/Extensions.Mcp.EndToEndTests --logger "console;verbosity=detailed"
### Azure Functions Logs

The test fixtures start Azure Functions hosts with `--verbose` flag. Host logs are captured in test output.

## Test Data

### Available Tools

**InProc Server (TestApp):**
- `getsnippets` - Retrieve code snippets
- `savesnippet` - Save code snippets  
- `searchSnippets` - Search through snippets

**Isolated Worker Server (TestAppIsolated):**
- `HappyFunction` - Demo function with multiple parameters
- `SingleArgumentFunction` - Single parameter function
- `SingleArgumentWithDefaultFunction` - Function with optional parameter
- `getsnippets` - Retrieve code snippets
- `savesnippet` - Save code snippets
- `searchsnippets` - Search through snippets

## Contributing

When adding new E2E tests:

1. **Follow the established patterns**: Use base classes for shared functionality
2. **Test both server types**: Create tests for both InProc and Isolated Worker
3. **Test multiple transports**: Use `[Theory]` with transport mode parameters
4. **Add appropriate documentation**: Update this README if adding new test categories
5. **Use TestUtility methods**: Leverage existing utilities for common operations

### Adding New Test Categories

1. Create base test class in `ProtocolTests/` or `InvocationTests/`
2. Create server-specific implementations in `InProc/` and `Default/` folders
3. Add fixture classes if needed in `Fixtures/`
4. Update this README with usage examples
