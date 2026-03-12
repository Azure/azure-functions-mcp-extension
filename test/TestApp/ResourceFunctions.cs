using Microsoft.Azure.Functions.Extensions.Mcp;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace TestApp;

public class ResourceFunctions
{
    [FunctionName("GetTextResource")]
    public static FileResourceContents GetTextResource(
        [McpResourceTrigger("file://resources/readme.txt", "ReadMe", Description = "Application readme file", MimeType = "text/plain")]
        string context,
        ILogger logger)
    {
        logger.LogInformation("Reading text resource from resources/readme.txt");

        return new FileResourceContents
        {
            Path = Path.Combine(Directory.GetCurrentDirectory(), "resources", "readme.txt")
        };
    }

    [FunctionName("GetBinaryResource")]
    public static FileResourceContents GetBinaryResource(
        [McpResourceTrigger("file://resources/logo.png", "Logo", Description = "Application logo", MimeType = "image/png")] 
        string context,
        ILogger logger)
    {
        logger.LogInformation("Reading binary resource from resources/logo.png");

        return new FileResourceContents
        {
            Path = Path.Combine(Directory.GetCurrentDirectory(), "resources", "logo.png")
        };
    }
}