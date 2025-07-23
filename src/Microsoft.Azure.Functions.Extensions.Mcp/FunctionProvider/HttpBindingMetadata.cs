// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Azure.WebJobs.Extensions.Http;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp;

internal sealed class HttpBindingMetadata
{
    public string Name { get; } = "request";

    public string Type { get; } = "httpTrigger";

    public string Direction { get; } = "In";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Route { get; set; }

    public AuthorizationLevel AuthLevel { get; set; } = AuthorizationLevel.Admin;

    public string[] Methods { get; set; } = ["GET", "POST"];
}
