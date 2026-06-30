// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ConformanceTestApp;

/// <summary>
/// Fixtures shared across the conformance tools / resources / prompts.
/// Names and payload shapes here MUST match the expectations baked into
/// https://github.com/modelcontextprotocol/conformance scenarios.
/// </summary>
internal static class ConformanceFixtures
{
    // Minimal 1x1 red PNG (decoded from a well-known base64 blob). The
    // conformance suite only checks that the data field is non-empty and
    // the mime type is present, but we use a real PNG so the bytes are
    // valid.
    public static readonly byte[] OnePixelPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");

    // 44-byte WAV header with zero PCM samples. Same reasoning as above.
    public static readonly byte[] EmptyWav = Convert.FromBase64String(
        "UklGRiQAAABXQVZFZm10IBAAAAABAAEAQB8AAEAfAAABAAgAZGF0YQAAAAA=");

    public const string StaticTextContent = "This is the content of the static text resource.";
    public const string SimpleTextToolResponse = "This is a simple text response for testing.";
    public const string SimplePromptText = "This is a simple prompt for testing.";
    public const string EmbeddedResourceText = "Embedded resource content for testing.";
    public const string ErrorToolMessage = "This tool intentionally returns an error for testing";
}
