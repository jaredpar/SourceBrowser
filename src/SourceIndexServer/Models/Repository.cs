#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
        foreach (var dir in Directory.EnumerateDirectories(rootPath))
        {
            if (TryParseRepository(dir, out var repository))
            {
                map[repository.Name] = repository;
            }
        }
    }

    public bool TryGetRepository(string name, [NotNullWhen(true)] out Repository? project)
    {
        return map.TryGetValue(name, out project);
    }

    public void AddRepository(string directory)
    {
        if (!TryParseRepository(directory, out var repository))
        {
            throw new ArgumentException($"Directory {directory} is not a valid repository");
        }
        map[repository.Name] = repository;
    }

    public IEnumerable<string> GetRepositoryNames() => map.Keys;

    public void Dispose()
    {
        foreach (var project in map.Values)
        {
            project.Index.Dispose();
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

public sealed class Repository(string name, string rootPath)
{
    public string Name { get; } = name;
    public string DirectoryName { get; } = Path.GetFileName(rootPath);
    public RepositoryIndex Index { get; } = new RepositoryIndex(rootPath);
}