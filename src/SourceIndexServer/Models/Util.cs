
namespace Microsoft.SourceBrowser.SourceIndexServer;

internal static class Util
{
    internal static bool InDocker { get { return Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";} }
}