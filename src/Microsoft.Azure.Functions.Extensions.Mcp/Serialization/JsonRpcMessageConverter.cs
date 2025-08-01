﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ModelContextProtocol.Protocol.Messages;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Serialization;

/// <summary>
/// JSON converter for JsonRpcMessage that handles polymorphic deserialization of different message types.
/// </summary>
internal sealed class JsonRpcMessageConverter : JsonConverter<JsonRpcMessage>
{
    /// <inheritdoc/>
    public override JsonRpcMessage? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token");
        }

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        // All JSON-RPC messages must have a jsonrpc property with value "2.0"
        if (!root.TryGetProperty("jsonrpc", out var versionProperty) ||
            versionProperty.GetString() != "2.0")
        {
            throw new JsonException("Invalid or missing jsonrpc version");
        }

        // Determine the message type based on the presence of id, method, and error properties
        bool hasId = root.TryGetProperty("id", out _);
        bool hasMethod = root.TryGetProperty("method", out _);
        bool hasError = root.TryGetProperty("error", out _);

        var rawText = root.GetRawText();

        // Messages with an id but no method are responses
        if (hasId && !hasMethod)
        {
            // Messages with an error property are error responses
            if (hasError)
            {
                return JsonSerializer.Deserialize<JsonRpcError>(rawText, options);
            }

            // Messages with a result property are success responses
            if (root.TryGetProperty("result", out _))
            {
                return JsonSerializer.Deserialize<JsonRpcResponse>(rawText, options);
            }

            throw new JsonException("Response must have either result or error");
        }

        // Messages with a method but no id are notifications
        if (hasMethod && !hasId)
        {
            return JsonSerializer.Deserialize<JsonRpcNotification>(rawText, options);
        }

        // Messages with both method and id are requests
        if (hasMethod && hasId)
        {
            return JsonSerializer.Deserialize<JsonRpcRequest>(rawText, options);
        }

        throw new JsonException("Invalid JSON-RPC message format");
    }

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, JsonRpcMessage value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case JsonRpcRequest request:
                JsonSerializer.Serialize(writer, request, options);
                break;
            case JsonRpcNotification notification:
                JsonSerializer.Serialize(writer, notification, options);
                break;
            case JsonRpcResponse response:
                JsonSerializer.Serialize(writer, response, options);
                break;
            case JsonRpcError error:
                JsonSerializer.Serialize(writer, error, options);
                break;
            default:
                throw new JsonException($"Unknown JSON-RPC message type: {value.GetType()}");
        }
    }
}