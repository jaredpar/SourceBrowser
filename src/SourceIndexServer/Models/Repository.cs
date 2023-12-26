#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryManager : IDisposable
{
    private readonly object _guard = new object();
    private readonly Dictionary<string, Repository> _repositoryMap = new Dictionary<string, Repository>();

    /// <summary>
    /// This is the root path for all of the on disk data
    /// </summary>
    public string RootPath { get; }

    public string IndexPath { get; }

    public string RepositoryPaths { get; } 

    public RepositoryManager(string rootPath)
    {
        RootPath = rootPath;
        IndexPath = Path.Combine(rootPath, "index");
        RepositoryPaths = Path.Combine(rootPath, "repositories");
        Directory.CreateDirectory(IndexPath);
        Directory.CreateDirectory(RepositoryPaths);
        LoadRepositoryMapNoLock();

        void LoadRepositoryMapNoLock()
        {
            foreach (var dir in Directory.EnumerateDirectories(RepositoryPaths))
            {
                if (TryParseRepository(dir, out var repoIndexPath))
                {
                    var repository = new Repository(dir, repoIndexPath);
                    _repositoryMap[repository.Name] = repository;
                }
            }
        }

        bool TryParseRepository(string repoPath, [NotNullWhen(true)] out string? repoIndexPath)
        {
            try
            {
                var index = Path.Combine(repoPath, "index.txt");
                if (!File.Exists(index))
                {
                    var indexName = File.ReadAllText(index).Trim();
                    repoIndexPath = Path.Combine(IndexPath, indexName);
                    if (Directory.Exists(repoIndexPath))
                    {
                        return true;
                    }
                }

                repoIndexPath = null;
                return false;
            }
            catch (Exception)
            {
                repoIndexPath = null;
                return false;
            }
        }
    }

    public bool TryGetRepository(string name, [NotNullWhen(true)] out Repository? project)
    {
        lock (_guard)
        {
            return _repositoryMap.TryGetValue(name, out project);
        }
    }

    public Repository AddOrUpdateRepository(string name, string indexName)
    {
        Repository? oldRepository = null;
        Repository newRepository = new Repository(name, indexName);
        lock (_guard)
        {
            _ = _repositoryMap.TryGetValue(name, out oldRepository);
            _repositoryMap[name] = newRepository;
            File.WriteAllLines(
                Path.Combine(newRepository.RepositoryPath, "index.txt"),
                [indexName]);
        }

        // HACK: make async
        if (oldRepository is not null)
        {
            DeleteNoLock(oldRepository);
        }

        return newRepository;
    }

    private void DeleteNoLock(Repository repository)
    {
        repository.RepositoryIndex.Dispose();
        try
        {
            Directory.Delete(repository.RepositoryPath, recursive: true);
        }
        catch (Exception)
        {
            // Nothing to do if it fails
        }
    }

    public void DeleteRangeAsync(IEnumerable<string> repositoryNames)
    {
        lock (_guard)
        {
            foreach (var name in repositoryNames)
            {
                if (_repositoryMap.Remove(name, out var repository))
                {
                    DeleteNoLock(repository);
                }
            }
        }
    }

    public IEnumerable<string> GetRepositoryNames()
    {
        lock(_guard) 
        {
            return _repositoryMap.Keys.ToArray();
        }
    }

    public void Dispose()
    {
        lock (_guard)
        {
            foreach (var repository in _repositoryMap.Values)
            {
                repository.RepositoryIndex.Dispose();
            }
        }
    }
}

public sealed class Repository(string repositoryPath, string indexPath)
{
    public string Name { get; } = Path.GetFileName(repositoryPath);
    public string RepositoryPath { get; } = repositoryPath;
    public string IndexName { get; } = Path.GetFileName(indexPath);
    public string IndexPath { get; } = indexPath;
    public RepositoryIndex RepositoryIndex { get; } = new RepositoryIndex(indexPath);
}