#nullable enable
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR.Protocol;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryManager : IDisposable
{
    private sealed class SourceInfo(string sourceName, string compilerLogFilePath)
    {
        public string SourceName { get; } = sourceName;
        public string? CompilerLogKey { get; set; }
        public string CompilerLogFilePath { get; }  = compilerLogFilePath;
        public string Directory => Path.GetDirectoryName(CompilerLogFilePath)!;
    }

    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(initialCount: 1, maxCount: 1);
    private readonly Dictionary<string, SourceInfo> _sourceMap = new Dictionary<string, SourceInfo>();
    private readonly ILogger<RepositoryManager> _logger;
    private readonly string _rootPath;
    private readonly string _sourcePath;
    private readonly string _indexPath;

    private RepositoryIndex _currentRepositoryIndex;

    public RepositoryIndex CurrentRepositoryIndex => _currentRepositoryIndex;
    public string RootPath => _rootPath;
    public string IndexPath => _indexPath;

    public RepositoryManager(string rootPath, ILogger<RepositoryManager> logger)
    {
        _logger = logger;
        _rootPath = rootPath;
        _indexPath = Path.Combine(rootPath, "index");
        _sourcePath = Path.Combine(rootPath, "source");
        Directory.CreateDirectory(_indexPath);
        Directory.CreateDirectory(_sourcePath);

        foreach (var dir in Directory.EnumerateDirectories(_indexPath))
        {
            if (File.Exists(Path.Combine(dir, "Projects.txt")))
            {
                _currentRepositoryIndex = new RepositoryIndex(dir);
                _logger.LogInformation($"Using index {_currentRepositoryIndex.contentName} at {dir}");
                break;
            }
        }

        if (_currentRepositoryIndex is null)
        {
            _logger.LogInformation("No index found");
            _currentRepositoryIndex = RepositoryIndex.Empty;
        }
    }

    public async Task ReplaceIndexAsync(string indexName)
    {
        Debug.Assert(!Path.IsPathRooted(indexName));
        var indexPath = Path.Combine(_indexPath, indexName);
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
 
    public async Task<string?> GetCompilerLogKey(string sourceName, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (_sourceMap.TryGetValue(sourceName, out var sourceInfo))
            {
                return sourceInfo.CompilerLogKey;
            }

            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task ReplaceCompilerLogAsync(string sourceName, string compilerLogKey, Stream compilerLogStream, CancellationToken cancellationToken)
    {
        Debug.Assert(!Path.IsPathRooted(sourceName));
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (!_sourceMap.TryGetValue(sourceName, out var sourceInfo))
            {
                sourceInfo = new SourceInfo(sourceName, Path.Combine(_sourcePath, sourceName, "build.complog"));
                _sourceMap[sourceName] = sourceInfo;
            }

            Directory.CreateDirectory(sourceInfo.Directory);
            sourceInfo.CompilerLogKey = compilerLogKey;
            using var stream = new FileStream(sourceInfo.CompilerLogFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await compilerLogStream.CopyToAsync(stream, cancellationToken);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task DoWithCompilerLogs(Func<IEnumerable<string>, Task> func, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var compilerLogFilePaths = _sourceMap.Values
                .Select(x => x.CompilerLogFilePath)
                .ToList();
            await func(compilerLogFilePaths);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void Dispose()
    {
        _currentRepositoryIndex.Dispose();
    }
}
