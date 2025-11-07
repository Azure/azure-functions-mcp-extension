// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Builder;

/// <summary>
/// Represents the type information for a property used within the MCP tool, including its name and whether it is an
/// array type.
/// </summary>
/// <param name="TypeName">The name of the property type. This value identifies the kind of data represented, such as "string", "number",
/// "integer", "boolean", or "object".</param>
/// <param name="IsArray">Indicates whether the property type represents an array. Specify <see langword="true"/> for array types; otherwise,
/// <see langword="false"/>.</param>
/// <param name="EnumValues">Optional collection of enum values when the property type represents an enum. Specify the valid enum values. Otherwise, [] </param>
public sealed record McpToolPropertyType(string TypeName, IReadOnlyList<string> EnumValues, bool IsArray = false)
{
    private const string StringTypeName = "string";
    private const string ObjectTypeName = "object";
    private const string NumberTypeName = "number";
    private const string IntegerTypeName = "integer";
    private const string BooleanTypeName = "boolean";

    private static McpToolPropertyType? _string;
    private static McpToolPropertyType? _stringArray;

    private static McpToolPropertyType? _number;
    private static McpToolPropertyType? _numberArray;

    private static McpToolPropertyType? _integer;
    private static McpToolPropertyType? _integerArray;

    private static McpToolPropertyType? _boolean;
    private static McpToolPropertyType? _booleanArray;

    private static McpToolPropertyType? _object;
    private static McpToolPropertyType? _objectArray;

    public McpToolPropertyType(string typeName, bool isArray = false)
       : this(typeName, [], isArray)
    {
    }

    /// <summary>
    /// Gets a property type value that represents a string MCP property.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for string properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType String => _string ??= new(StringTypeName);

    /// <summary>
    /// Gets the property type representing an array of <see cref="String"/>.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for string arrays properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType StringArray => _stringArray ??= new(StringTypeName, true);

    /// <summary>
    /// Gets a property type value that represents a number MCP property.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for number properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType Number => _number ??= new(NumberTypeName);

    /// <summary>
    /// Gets the property type representing an array of <see cref="Number"/>.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for number arrays properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType NumberArray => _numberArray ??= new(NumberTypeName, true);

    /// <summary>
    /// Gets a property type value that represents an integer MCP property.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for integer properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType Integer => _integer ??= new(IntegerTypeName);

    /// <summary>
    /// Gets the property type representing an array of <see cref="Integer"/>.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for integer arrays properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType IntegerArray => _integerArray ??= new(IntegerTypeName, true);

    /// <summary>
    /// Gets a property type value that represents a boolean MCP property type.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for boolean properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType Boolean => _boolean ??= new(BooleanTypeName);

    /// <summary>
    /// Gets the property type representing an array of <see cref="Boolean"/>.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for boolean arrays properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType BooleanArray => _booleanArray ??= new(BooleanTypeName, true);

    /// <summary>
    /// Gets a property type value that represents an object MCP property type.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for object properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType Object => _object ??= new(ObjectTypeName);

    /// <summary>
    /// Gets the property type representing an array of <see cref="Object"/>.
    /// </summary>
    /// <remarks>This property provides a reusable type definition for object arrays properties.
    /// The returned type is immutable and can be shared across multiple components.</remarks>
    public static McpToolPropertyType ObjectArray => _objectArray ??= new(ObjectTypeName, true);

    /// <summary>
    /// Returns a new instance of the property type representing an array of the current type.
    /// </summary>
    /// <returns>A <see cref="McpToolPropertyType"/> instance configured as an array of the current type.</returns>
    public McpToolPropertyType AsArray() => new(TypeName, EnumValues, true);

    /// <summary>
    /// Checks if the current property type represents an enum.
    /// </summary>
    /// <returns><c>true</c> if the current property type represents an enum; otherwise, <c>false</c>.</returns>
    public bool IsEnum => EnumValues is { Count: > 0 };

    /// <summary>
    /// Determines whether this instance and another specified <see cref="McpToolPropertyType"/> object have the same value.
    /// </summary>
    /// <param name="other">The <see cref="McpToolPropertyType"/> to compare with this instance.</param>
    /// <returns><c>true</c> if the value of <paramref name="other"/> is the same as this instance; otherwise, <c>false</c>.</returns>
    public bool Equals(McpToolPropertyType? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        // Compare the basic properties
        if (TypeName != other.TypeName || IsArray != other.IsArray)
        {
            return false;
        }

        // Compare EnumValues using sequence equality
        return EnumValuesEqual(EnumValues, other.EnumValues);
    }

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(TypeName);
        hash.Add(IsArray);

        // Add each enum value to the hash
        if (EnumValues is not null)
        {
            foreach (var value in EnumValues)
            {
                hash.Add(value);
            }
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares two enum value collections for sequence equality.
    /// </summary>
    /// <param name="first">The first collection to compare.</param>
    /// <param name="second">The second collection to compare.</param>
    /// <returns><c>true</c> if the collections contain the same values in the same order; otherwise, <c>false</c>.</returns>
    private static bool EnumValuesEqual(IReadOnlyList<string>? first, IReadOnlyList<string>? second)
    {
        // Handle null cases
        if (first is null && second is null)
        {
            return true;
        }

        if (first is null || second is null)
        {
            return false;
        }

        // Compare counts first for efficiency
        if (first.Count != second.Count)
        {
            return false;
        }

        // Compare each element
        for (int i = 0; i < first.Count; i++)
        {
            if (first[i] != second[i])
            {
                return false;
            }
        }

        return true;
    }
}
