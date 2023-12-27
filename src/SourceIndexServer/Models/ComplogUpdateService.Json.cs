#nullable enable

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using Basic.Azure.Pipelines;
using Microsoft.VisualBasic;

namespace Microsoft.SourceBrowser.SourceIndexServer.Json;

public sealed class UpdateJson
{
    public List<AzurePipelineJson>? Pipelines { get; set; }
    public List<FileJson>? Files { get; set; }
}

public sealed class AzurePipelineJson
{
    public string? SourceName { get; set; }
    public string? Organization { get; set; }
    public string? Project { get; set; }
    public int Definition { get; set; }
    public string? ArtifactName { get; set; }
    public string? FileName { get; set; }
}

public sealed class FileJson
{
    public string? SourceName { get; set;}
    public string? FilePath { get; set; }
}