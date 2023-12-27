#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryManager : IDisposable
{
    private readonly object _sourceGuard = new object();

    private RepositoryIndex _currentRepositoryIndex;

    /// <summary>
    /// This is the root path for all of the on disk data
    /// </summary>
    public string RootPath { get; }

    /// <summary>
    /// The folder where the index is stored
    /// </summary>
    public string IndexPath { get; }

    /// <summary>
    /// The folder where the original source complogs are stored
    /// </summary>
    public string SourcePath { get; } 

    public RepositoryIndex CurrentRepositoryIndex => _currentRepositoryIndex;

    public RepositoryManager(string rootPath)
    {
        RootPath = rootPath;
        IndexPath = Path.Combine(rootPath, "index");
        SourcePath = Path.Combine(rootPath, "source");
        Directory.CreateDirectory(IndexPath);
        Directory.CreateDirectory(SourcePath);

        foreach (var dir in Directory.EnumerateDirectories(IndexPath))
        {
            if (File.Exists(Path.Combine(dir, "Projects.txt")))
            {
                _currentRepositoryIndex = new RepositoryIndex(dir);
                break;
            }   
        }

        if (_currentRepositoryIndex is null)
        {
            _currentRepositoryIndex = RepositoryIndex.Empty;
        }
    }

    public async Task ReplaceIndexAsync(string indexName)
    {
        Debug.Assert(!Path.IsPathRooted(indexName));
        var indexPath = Path.Combine(IndexPath, indexName);
        var index = new RepositoryIndex(indexPath);
        await index.completed;
        var old = Interlocked.Exchange(ref _currentRepositoryIndex, index);
        old.Dispose();
        _ = Task.Run(() => DeleteOldIndex());

        void DeleteOldIndex()
        {
            if (string.IsNullOrEmpty(old.contentPath))
            {
                return;
            }

            try
            {
                Directory.Delete(old.contentPath, recursive: true);
            }
            catch
            {
                // ignored
            }
        }
    }

    public async Task<bool> ReplaceCompilerLogAsync(string sourceName, Stream complogStream, CancellationToken cancellationToken)
    {
        Debug.Assert(!Path.IsPathRooted(sourceName));
        var sourcePath = Path.Combine(SourcePath, sourceName);
        Directory.CreateDirectory(sourcePath);
        var sha = SHA256.Create();
        var hash = sha.ComputeHash(complogStream);
        complogStream.Position = 0;

        var hashText = GetHashText(hash);
        var hashFilePath = Path.Combine(sourcePath, "hash.txt");
        if (File.Exists(hashFilePath))
        {
            var existingHash = await File.ReadAllTextAsync(hashFilePath, cancellationToken);
            if (existingHash == hashText)
            {
                return false;
            }
        }

        using var stream = new FileStream(Path.Combine(sourcePath, "build.complog"), FileMode.Create, FileAccess.Write, FileShare.None);
        await complogStream.CopyToAsync(stream, cancellationToken);
        await File.WriteAllTextAsync(hashFilePath, hashText);
        return true;

        static string GetHashText(ReadOnlySpan<byte> span)
        {
            var builder = new StringBuilder();
            foreach (var b in span)
            {
                builder.Append($"{b:X2}");
            }

            return builder.ToString();
        }
    }

    public IEnumerable<(string SourceName, string CompilerLogFilePath)> GetCompilerLogs()
    {
        foreach (var sourcePath in Directory.EnumerateDirectories(SourcePath))
        {
            var compilerLogFilePath = Path.Combine(sourcePath, "build.complog");
            if (File.Exists(compilerLogFilePath))
            {
                yield return (Path.GetFileName(sourcePath), compilerLogFilePath);
            }
        }
    }

    public void Dispose()
    {
        _currentRepositoryIndex.Dispose();
    }
}
