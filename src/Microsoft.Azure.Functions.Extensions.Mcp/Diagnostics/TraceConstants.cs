using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Functions.Extensions.Mcp.Diagnostics
{
    internal class TraceConstants
    {
        public const string ExtActivitySource = "Azure.Functions.Extensions.Mcp";
        public const string ExtActivitySourceVersion = "1.0.0.0";

        public const string AttributeExceptionEventName = "exception";
        public const string AttributeExceptionType = "exception.type";
        public const string AttributeExceptionMessage = "exception.message";
        public const string AttributeExceptionStacktrace = "exception.stacktrace";
        public const string AttributeExceptionEscaped = "exception.escaped";
    }
}
