#nullable enable

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Basic.Azure.Pipelines;
using Microsoft.VisualBasic;
using Octokit;

namespace Microsoft.SourceBrowser.SourceIndexServer.Json;

public sealed class SourcesJson
{
    public required List<SourceJson> Sources { get; set; }
}

public sealed class SourceJson
{
    public required string Name { get; set; }
    public PipelineJson? Pipeline { get; set;} 
    public FileJson? File { get; set;} 
    public WorkflowJson? Workflow { get; set;}
}

public sealed class PipelineJson
{
    public required string Organization { get; set; }
    public required string Project { get; set; }
    public int Definition { get; set; }
    public required string ArtifactName { get; set; }
    public required string FileName { get; set; }
}

public sealed class FileJson
{
    public required string FilePath { get; set; }
}

public sealed class WorkflowJson
{
    public required string Owner { get; set; }
    public required string Repo { get; set; }
    public required string WorkflowFileName { get; set; }
    public required string ArtifactName { get; set; }
    public required string FileName { get; set; }
}
