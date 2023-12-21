#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SourceBrowser.Common;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public sealed class RepositoryManager : IDisposable
{
    private string RootPath { get; }

    /// <summary>
    /// This maintains a map of the repository name to the <see cref="Repository"/> 
    /// </summary>
    private ConcurrentDictionary<string, Repository> map { get; } = new ConcurrentDictionary<string, Repository>();

    public RepositoryManager(string rootPath)
    {
        RootPath = rootPath;
        Directory.CreateDirectory(rootPath);
        LoadRepositoryMap();
    }

    public bool TryGetRepository(string name, [NotNullWhen(true)] out Repository? project)
    {
        return map.TryGetValue(name, out project);
    }

    public void AddOrUpdateRepository(string repositoryName, string directory)
    {
        var repository = new Repository(repositoryName, directory);
        Repository? existing = null;
        map.AddOrUpdate(
            repository.Name,
            repository,
            (_, e) =>
            {
                existing = e;
                return repository;
            });
        if (existing is not null)
        {
            existing.Index.Dispose();

            try
            {
                SaveRepositoryMap();
                Directory.Delete(existing.Directory, recursive: true);
            }
            catch
            {
                // Nothing to do 
            }
        }
    }

    public IEnumerable<string> GetRepositoryNames() => map.Keys;

    public void Dispose()
    {
        foreach (var project in map.Values)
        {
            project.Index.Dispose();
        }
    }

    private void LoadRepositoryMap()
    {
        var filePath = Path.Combine(RootPath, "map.txt");
        if (File.Exists(filePath))
        {
            using var reader = new StreamReader(filePath, Encoding.UTF8);
            while (reader.ReadLine() is string line)
            {
                var parts = line.Split(':', count: 2, StringSplitOptions.RemoveEmptyEntries);
                var repository = new Repository(parts[0], parts[1]);
                map[repository.Name] = repository;
            }
        }
        else
        {
            foreach (var dir in Directory.EnumerateDirectories(RootPath))
            {
                if (TryParseRepository(dir, out var repository))
                {
                    map[repository.Name] = repository;
                }
            }
        }
    }

    private void SaveRepositoryMap()
    {
        using var writer = new StreamWriter(Path.Combine(RootPath, "map.txt"), append: false, Encoding.UTF8);
        foreach (var pair in this.map)
        {
            writer.WriteLine($"{pair.Key}:{pair.Value.DirectoryName}");
        }
    }

    private static bool TryParseRepository(string dir, [NotNullWhen(true)] out Repository? repository)
    {
        if (!File.Exists(Path.Combine(dir, "Projects.txt")))
        {
            repository = null;
            return false;
        }

        var nameFilePath = Path.Combine(dir, "name.txt");
        string name;
        if (File.Exists(nameFilePath))
        {
            name = File.ReadAllText(nameFilePath).Trim();
        }
        else
        {
            name = Path.GetFileName(dir);
        }

        repository = new Repository(name, dir);
        return true;
    }
}

public sealed class Repository(string name, string directory)
{
    public string Name { get; } = name;
    public string Directory { get; } = directory;
    public string DirectoryName { get; } = Path.GetFileName(directory);
    public RepositoryIndex Index { get; } = new RepositoryIndex(directory);
}