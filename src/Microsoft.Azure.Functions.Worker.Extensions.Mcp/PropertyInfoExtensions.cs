// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp.Reflection;

internal static class PropertyInfoExtensions
{
    /// <summary>
    /// Checks if the given property has a <see cref="DescriptionAttribute"/>.
    /// </summary>
    /// <returns>A string of the description if present, otherwise null.</returns>
    public static string? GetDescription(this PropertyInfo property)
    {
        return property.GetCustomAttribute<DescriptionAttribute>()?.Description;
    }

    /// <summary>
    /// Checks if the given property has a <see cref="RequiredMemberAttribute"/>.
    /// </summary>
    /// <returns>Returns true if the property is required, otherwise false.</returns>
    public static bool IsRequired(this PropertyInfo property)
    {
        return property.GetCustomAttribute<RequiredMemberAttribute>() is not null;
    }
}
