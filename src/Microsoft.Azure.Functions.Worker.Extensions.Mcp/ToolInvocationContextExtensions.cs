// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Azure.Functions.Worker.Extensions.Mcp
{
    public static class ToolInvocationContextExtensions
    {
        /// <summary>
        /// Attempts to retrieve the HTTP transport associated with the specified tool invocation context.
        /// </summary>
        /// <param name="context">The tool invocation context from which to obtain the HTTP transport. Cannot be null.</param>
        /// <param name="transport">When this method returns, contains the HTTP transport if one is available; otherwise, null.</param>
        /// <returns>true if an HTTP transport is available in the context; otherwise, false.</returns>
        public static bool TryGetHttpTransport(this ToolInvocationContext context, out HttpTransport? transport)
        {
            transport = context.Transport as HttpTransport;

            return transport is not null;
        }
    }
}
