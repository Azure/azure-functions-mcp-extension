using System.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.Functions.Extensions.Mcp.Protocol.Model;

namespace TestApp
{
    public class TestFunction
    {
        [FunctionName(nameof(Test))]
        public object Test([McpToolTrigger("foo", "A foolish tool.")] CallToolRequestParams message)
        {
            return $"Echo {message.Arguments.First().Value}.";
        }
    }
}
