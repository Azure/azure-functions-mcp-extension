// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp;

/// <summary>
/// Represents a file-based resource result. When returned from a resource function,
/// the framework reads the file and converts it to the appropriate resource content type
/// based on the MIME type from the <c>McpResourceTriggerAttribute</c>.
/// </summary>
/// <remarks>
/// <para>
/// Relative paths are resolved against <see cref="AppContext.BaseDirectory"/>.
/// Absolute paths are used as-is.
/// </para>
/// <para>
/// The MIME type from the trigger attribute determines how the file is read:
/// <list type="bullet">
///   <item><c>text/*</c>, <c>application/json</c>, <c>application/xml</c>, <c>application/javascript</c> — read as UTF-8 text</item>
///   <item><c>*+json</c>, <c>*+xml</c> structured syntax suffixes — read as UTF-8 text</item>
///   <item>All other types (images, PDFs, etc.) — read as bytes (base64-encoded)</item>
///   <item>If no MIME type is set on the attribute, defaults to text</item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Function(nameof(GetImage))]
/// public FileResourceContents GetImage(
///     [McpResourceTrigger("file://logo.png", "logo", MimeType = "image/png")]
///     ResourceInvocationContext context)
/// {
///     return new FileResourceContents
///     {
///         Path = "assets/logo.png"
///     };
/// }
/// </code>
/// </example>
public sealed class FileResourceContents
{
    /// <summary>
    /// Gets or sets the file path to read. Relative paths are resolved against
    /// <see cref="AppContext.BaseDirectory"/>.
    /// </summary>
    public required string Path { get; set; }
}
