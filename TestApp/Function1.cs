using Microsoft.Azure.WebJobs;
using Microsoft.Azure.Functions.Extensions.Mcp;

namespace TestApp
{
    public class TestFunction
    {
        [FunctionName(nameof(Test))]
        public object Test([McpToolTrigger("foo", "A foolish tool.")] object message)
        {
            return $"Echo {message}.";
        }
    }
}
