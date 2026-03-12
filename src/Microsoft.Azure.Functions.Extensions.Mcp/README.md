# Microsoft.Azure.Functions.Extensions.Mcp

This package provides triggers and bindings to support exposing Azure Functions applications as a Model Context Protocol (MCP) server.

## Resource return types

Resource handlers can return one of the following shapes:

- `string` for text resources
- `byte[]` for binary resources
- `FileResourceContents` when you want the framework to read a file and materialize the correct MCP resource content automatically

`TextResourceContents` and `BlobResourceContents` are internal MCP payload shapes produced by the framework and are not intended as direct function return types.

## FileResourceContents

Use `FileResourceContents` when your resource maps directly to a file on disk and you do not want to perform file I/O in your function body.

```csharp
[FunctionName("GetWidgetHtml")]
public static FileResourceContents GetWidgetHtml(
	[McpResourceTrigger(
		"ui://widget/welcome.html",
		"WelcomeWidget",
		MimeType = "text/html+skybridge")]
	string context,
	ILogger logger)
{
	logger.LogInformation("Serving widget HTML from disk");

	return new FileResourceContents
	{
		Uri = "ui://widget/welcome.html",
		MimeType = "text/html+skybridge",
		Path = Path.Combine(Directory.GetCurrentDirectory(), "ui", "welcome.html"),
		Meta =
		{
			["openai/widgetPrefersBorder"] = true
		}
	};
}
```

When a function returns `FileResourceContents`, the framework:

- reads the file at `Path`
- uses `MimeType` to decide whether the content should be treated as text or binary
- converts text-like MIME types to `TextResourceContents`
- converts binary MIME types to `BlobResourceContents` with base64-encoded payloads
- preserves `Uri`, `MimeType`, and `Meta` on the final MCP response

If `MimeType` is omitted on `FileResourceContents`, the extension falls back to the trigger attribute MIME type and then to file-extension-based content type detection.
