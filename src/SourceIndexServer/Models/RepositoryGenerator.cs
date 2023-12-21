#nullable enable
namespace Microsoft.SourceBrowser.SourceIndexServer;

/// <summary>
/// This class is responsible for generating the html / js pages for the given
/// project onto disk.
/// </summary>
public sealed class RepositoryGenerator(string rootPath, string htmlGeneratorFilePath, ILogger<RepositoryGenerator> logger)
{
    /// <summary>
    /// The path to the html generator tool that is used to generate the project
    /// content.
    /// </summary>
    public string HtmlGeneratorFilePath { get; } = htmlGeneratorFilePath;

    private string RootPath { get; } = rootPath;

    private string ScratchPath { get; } = Path.Combine(rootPath, ".scratch");

    /// <summary>
    /// Generates the project content and returns the path that it was generated
    /// to.
    /// </summary>
    public async Task<string> Generate(Stream complogStream)
    {
        var dirName = Guid.NewGuid().ToString();
        var dirPath = Path.Combine(RootPath, dirName);
        if (Directory.Exists(dirPath))
        {
            throw new ArgumentException($"Directory {dirName} already exists");
        }

        var complogFilePath = await SaveToScratch(complogStream);
        try
        {
            await RunHtmlGenerator(complogFilePath, dirPath);
        }
        finally
        {
            File.Delete(complogFilePath);
        }

        return dirPath;
    }

    private async Task RunHtmlGenerator(string complogFilePath, string destDirectory)
    {
        string[] args =
        [
            "exec",
            HtmlGeneratorFilePath,
            complogFilePath,
            $"/out:{destDirectory}"
        ];

        logger.LogInformation($"Generating {complogFilePath} to {destDirectory}");
        var result = await ProcessUtil.RunAsync("dotnet", args, workingDirectory: RootPath);
        if (!result.Succeeded)
        {
            logger.LogError($"Html generator failed: {result.StandardOut} {result.StandardError}");
            throw new Exception($"Html generator failed");
        }
    }

    private async Task<string> SaveToScratch(Stream stream)
    {
        Directory.CreateDirectory(ScratchPath);
        var filePath = Path.Combine(ScratchPath, $"{Guid.NewGuid()}.complog");
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream);
        return filePath;
    }
}