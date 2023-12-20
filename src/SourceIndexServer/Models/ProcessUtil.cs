#nullable enable

using System.Diagnostics;

namespace Microsoft.SourceBrowser.SourceIndexServer;

internal readonly struct ProcessResult
{
    internal int ExitCode { get; }
    internal string StandardOut { get; }
    internal string StandardError { get; }

    internal bool Succeeded => ExitCode == 0;

    internal ProcessResult(int exitCode, string standardOut, string standardError)
    {
        ExitCode = exitCode;
        StandardOut = standardOut;
        StandardError = standardError;
    }
}

internal static class ProcessUtil
{
    internal static async Task<ProcessResult> RunAsync(
        string fileName,
        string[] args,
        string? workingDirectory = null)
    {
        var info = new ProcessStartInfo(fileName, args)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };

        var process = Process.Start(info)!;
        var standardOut = process.StandardOutput.ReadToEnd();
        var standardError = process.StandardError.ReadToEnd();
        await process.WaitForExitAsync();

        return new ProcessResult(
            process.ExitCode,
            standardOut,
            standardError);
    }
}
