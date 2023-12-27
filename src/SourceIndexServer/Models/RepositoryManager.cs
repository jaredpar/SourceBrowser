#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryManager : IDisposable
{
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

    public async Task ReplaceIndex(string indexName)
    {
        Debug.Assert(!Path.IsPathRooted(indexName));
        var indexPath = Path.Combine(IndexPath, indexName);
        var index = new RepositoryIndex(indexPath);
        await index.completed;
        var old = Interlocked.Exchange(ref _currentRepositoryIndex, index);
        old.Dispose();
    }

    public void Dispose()
    {
        _currentRepositoryIndex.Dispose();
    }
}
