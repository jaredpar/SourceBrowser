using System.Text;

namespace Basic.Azure.Pipelines
{
    public readonly struct BuildKey : IEquatable<BuildKey>
    {
        private const char EscapeChar = '.';
        private const char SeparatorChar = '-';

        public string Organization { get; }
        public string Project { get; }
        public int Number { get; }

        public string BuildUri => DevOpsUtil.GetBuildUri(Organization, Project, Number);

        public string NameKey
        {
            get
            {
                var builder = new StringBuilder();
                AppendEscaped(Organization);
                builder.Append(SeparatorChar);
                AppendEscaped(Project);
                builder.Append(SeparatorChar);
                builder.Append(Number);
                return builder.ToString();

                void AppendEscaped(string word)
                {
                    foreach (var c in word)
                    {
                        if (c == SeparatorChar || c == EscapeChar)
                        {
                            builder.Append(EscapeChar);
                        }
                        builder.Append(c);
                    }
                }
            }
        }

        public BuildKey(string organization, string project, int number)
        {
            if (organization is null)
            {
                throw new ArgumentNullException(nameof(organization));
            }

            if (project is null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            Organization = organization;
            Project = project;
            Number = number;
        }

        public BuildKey(Build build) : 
            this(DevOpsUtil.GetOrganization(build), build.Project.Name, build.Id)
        {
        }

        public static BuildKey FromNameKey(string nameKey)
        {
            var builder = new StringBuilder();
            var index = 0;

            return new BuildKey(ParseWord(), ParseWord(), int.Parse(ParseWord()));
            string ParseWord()
            {
                builder.Length = 0;
                while (index < nameKey.Length)
                {
                    switch (nameKey[index])
                    {
                        case EscapeChar:
                            builder.Append(nameKey[index + 1]);
                            index += 2;
                            break;
                        case SeparatorChar:
                            index++;
                            return builder.ToString();
                        default:
                            builder.Append(nameKey[index]);
                            index++;
                            break;
                    }
                }

                return builder.ToString();
            }
        }

        public static bool operator==(BuildKey left, BuildKey right) => left.Equals(right); 

        public static bool operator!=(BuildKey left, BuildKey right) => !left.Equals(right); 

        public static implicit operator BuildKey(Build build) => new BuildKey(build);

        public bool Equals(BuildKey other) =>
            other.Organization == Organization &&
            other.Project == Project &&
            other.Number == Number;

        public override bool Equals(object? other) => other is BuildKey key && Equals(key);

        public override int GetHashCode() => HashCode.Combine(Organization, Project, Number);

        public override string ToString() => $"{Organization} {Project} {Number}";
    }

    public readonly struct BuildAttemptKey : IEquatable<BuildAttemptKey>
    {
        public BuildKey BuildKey { get; }
        public int Attempt { get; }

        public string Organization => BuildKey.Organization;
        public string Project => BuildKey.Project;
        public int Number => BuildKey.Number;
        public string BuildUri => BuildKey.BuildUri;

        public BuildAttemptKey(string organization, string project, int number, int attempt)
        {
            BuildKey = new BuildKey(organization, project, number);
            Attempt = attempt;
        }

        public BuildAttemptKey(Build build, Timeline timeline)
        {
            BuildKey = new BuildKey(build);
            Attempt = timeline.GetAttempt();
        }

        public BuildAttemptKey(BuildKey buildKey, int attempt)
        {
            BuildKey = buildKey;
            Attempt = attempt;
        }

        public static bool operator==(BuildAttemptKey left, BuildAttemptKey right) => left.Equals(right); 

        public static bool operator!=(BuildAttemptKey left, BuildAttemptKey right) => !left.Equals(right); 

        public bool Equals(BuildAttemptKey other) =>
            other.BuildKey == BuildKey &&
            other.Attempt == Attempt;

        public override bool Equals(object? other) => other is BuildAttemptKey key && Equals(key);

        public override int GetHashCode() => HashCode.Combine(BuildKey, Attempt);

        public override string ToString() => $"{Organization} {Project} {Number} {Attempt}";
    }

    public readonly struct DefinitionKey : IEquatable<DefinitionKey>
    {
        public string Organization { get; }
        public string Project { get; }
        public int Id { get; }

        public string DefinitionUri => DevOpsUtil.GetDefinitionUri(Organization, Project, Id);

        public DefinitionKey(string organization, string project, int id)
        {
            Organization = organization;
            Project = project;
            Id = id;
        }

        public static bool operator==(DefinitionKey left, DefinitionKey right) => left.Equals(right); 

        public static bool operator!=(DefinitionKey left, DefinitionKey right) => !left.Equals(right); 

        public bool Equals(DefinitionKey other) =>
            other.Organization == Organization &&
            other.Project == Project &&
            other.Id == Id;

        public override bool Equals(object? other) => other is DefinitionKey key && Equals(key);

        public override int GetHashCode() => HashCode.Combine(Organization, Project, Id);

        public override string ToString() => $"{Organization} {Project} {Id}";
    }

    public readonly struct DefinitionNameKey : IEquatable<DefinitionNameKey>
    {
        public string Organization { get; }
        public string Project { get; }
        public string Name { get; }

        public DefinitionNameKey(string organization, string project, string name)
        {
            Organization = organization;
            Project = project;
            Name = name;
        }

        public static bool operator==(DefinitionNameKey left, DefinitionNameKey right) => left.Equals(right); 

        public static bool operator!=(DefinitionNameKey left, DefinitionNameKey right) => !left.Equals(right);

        public bool Equals(DefinitionNameKey other) =>
            other.Organization == Organization &&
            other.Project == Project &&
            other.Name == Name;

        public override bool Equals(object? other) => other is DefinitionNameKey key && Equals(key);

        public override int GetHashCode() => HashCode.Combine(Organization, Project, Name);

        public override string ToString() => $"{Organization} {Project} {Name}";
    }

    public readonly struct GitHubBuildInfo
    {
        public string Organization { get; }
        public string Repository { get; }
        public int? PullRequestNumber { get; }
        public string? TargetBranch { get; }

        public GitHubPullRequestKey? PullRequestKey => PullRequestNumber is int number
            ? new GitHubPullRequestKey(Organization, Repository, number)
            : (GitHubPullRequestKey?)null;

        public GitHubBuildInfo(
            string organization,
            string repository,
            int? pullRequestNumber,
            string? targetBranch)
        {
            Organization = organization;
            Repository = repository;
            PullRequestNumber = pullRequestNumber;
            TargetBranch = targetBranch;
        }

        public override string ToString() => $"{Organization} {Repository}";
    }

    public sealed class BuildInfo
    {
        public BuildKey BuildKey { get; }
        public GitHubBuildInfo? GitHubBuildInfo { get; }

        public string Organization => BuildKey.Organization;
        public string Project => BuildKey.Project;
        public int Number => BuildKey.Number;
        public string BuildUri => BuildKey.BuildUri;
        public GitHubPullRequestKey? PullRequestKey => GitHubBuildInfo?.PullRequestKey;

        public BuildInfo(
            string organization,
            string project,
            int buildNumber,
            GitHubBuildInfo? gitHubBuildInfo)
        {
            BuildKey = new BuildKey(organization, project, buildNumber);
            GitHubBuildInfo = gitHubBuildInfo;
        }

        public override string ToString() => $"{Organization} {Project} {Number}";
    }

    public sealed class DefinitionInfo
    {
        public DefinitionKey DefinitionKey { get; }
        public string Name { get; }

        public string Organization => DefinitionKey.Organization;
        public string Project => DefinitionKey.Project;
        public int Id => DefinitionKey.Id;
        public string DefinitionUri => DefinitionKey.DefinitionUri;

        public DefinitionInfo(DefinitionKey key, string name)
        {
            DefinitionKey = key;
            Name = name;
        }

        public DefinitionInfo(string organization, string project, int id, string name) 
            : this(new DefinitionKey(organization, project, id), name)
        {
        }

        public override string ToString() => $"{Project} {Name} {Id}";
    }

    public sealed class BuildAndDefinitionInfo
    {
        public BuildInfo BuildInfo { get; }
        public DefinitionInfo DefinitionInfo { get; }

        public string Organization => DefinitionInfo.Organization;
        public string Project => DefinitionInfo.Project;
        public int BuildNumber => BuildInfo.Number;
        public GitHubBuildInfo? GitHubBuildInfo => BuildInfo.GitHubBuildInfo;
        public string DefinitionName => DefinitionInfo.Name;
        public int DefinitionId => DefinitionInfo.Id;
        public string BuildUri => BuildKey.BuildUri;
        public BuildKey BuildKey => BuildInfo.BuildKey;
        public DefinitionKey DefinitionKey => DefinitionInfo.DefinitionKey;
        public GitHubPullRequestKey? PullRequestKey => GitHubBuildInfo?.PullRequestKey;

        public BuildAndDefinitionInfo(
            string organization,
            string project,
            int buildNumber,
            int definitionId,
            string definitionName,
            GitHubBuildInfo? gitHubBuildInfo)
        {
            BuildInfo = new BuildInfo(organization, project, buildNumber, gitHubBuildInfo);
            DefinitionInfo = new DefinitionInfo(organization, project, definitionId, definitionName);
        }

        public override string ToString() => $"{DefinitionName} {BuildNumber}";
    }

    public sealed class BuildResultInfo
    {
        public BuildAndDefinitionInfo BuildAndDefinitionInfo { get; }
        public DateTime QueueTime { get; }
        public DateTime? StartTime { get; }
        public DateTime? FinishTime { get; }
        public BuildResult BuildResult { get; }

        public BuildInfo BuildInfo => BuildAndDefinitionInfo.BuildInfo;
        public DefinitionInfo DefinitionInfo => BuildAndDefinitionInfo.DefinitionInfo;
        public GitHubBuildInfo? GitHubBuildInfo => BuildAndDefinitionInfo.GitHubBuildInfo;
        public GitHubPullRequestKey? PullRequestKey => BuildInfo.PullRequestKey;
        public string Organization => BuildInfo.Organization;
        public string Project => BuildInfo.Project;
        public int Number => BuildInfo.Number;
        public string DefinitionName => DefinitionInfo.Name;
        public BuildKey BuildKey => BuildInfo.BuildKey;
        public string BuildUri => BuildInfo.BuildUri;

        public BuildResultInfo(
            BuildAndDefinitionInfo buildAndDefinitionInfo,
            DateTime queueTime,
            DateTime? startTime,
            DateTime? finishTime,
            BuildResult buildResult)
        {
            BuildAndDefinitionInfo = buildAndDefinitionInfo;
            QueueTime = queueTime;
            StartTime = startTime;
            FinishTime = finishTime;
            BuildResult = buildResult;
        }

        public override string ToString() => $"{Organization} {Project} {Number}";
    }
    public readonly struct GitHubIssueKey : IEquatable<GitHubIssueKey>
    {
        public string Organization { get; }

        public string Repository { get; }

        public int Number { get; }

        public string IssueUri => $"https://github.com/{Organization}/{Repository}/issues/{Number}";

        public GitHubIssueKey(string organization, string repository, int number)
        {
            Organization = organization;
            Repository = repository;
            Number = number;
        }

        public static bool operator==(GitHubIssueKey left, GitHubIssueKey right) => left.Equals(right);

        public static bool operator!=(GitHubIssueKey left, GitHubIssueKey right) => !left.Equals(right);

        public bool Equals(GitHubIssueKey other) =>
            other.Organization == Organization &&
            other.Repository == Repository &&
            other.Number == Number;

        public override bool Equals(object? obj) => obj is GitHubIssueKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Organization, Repository, Number);

        public override string ToString() => $"{Organization}/{Repository}/{Number}";

        public static bool TryCreateFromUri(string uri, out GitHubIssueKey issueKey)
        {
            if (Uri.TryCreate(uri, UriKind.Absolute, out var result))
            {
                var items = result.PathAndQuery.Split('/', StringSplitOptions.RemoveEmptyEntries);
                if (items.Length >= 4 && int.TryParse(items[3], out var number))
                {
                    issueKey = new GitHubIssueKey(items[0], items[1], number);
                    return true;
                }

            }

            issueKey = default;
            return false;
        }
    }

    public readonly struct GitHubPullRequestKey : IEquatable<GitHubPullRequestKey>
    {
        public string Organization { get; }

        public string Repository { get; }

        public int Number { get; }

        public string PullRequestUri => GetPullRequestUri(Organization, Repository, Number);

        public GitHubPullRequestKey(string organization, string repository, int number)
        {
            Organization = organization;
            Repository = repository;
            Number = number;
        }

        public GitHubIssueKey ToIssueKey() => new GitHubIssueKey(Organization, Repository, Number);

        public static string GetPullRequestUri(string organization, string repository, int number) => 
            $"https://github.com/{organization}/{repository}/pull/{number}";

        public static bool operator==(GitHubPullRequestKey left, GitHubPullRequestKey right) => left.Equals(right);

        public static bool operator!=(GitHubPullRequestKey left, GitHubPullRequestKey right) => !left.Equals(right);

        public bool Equals(GitHubPullRequestKey other) =>
            other.Organization == Organization &&
            other.Repository == Repository &&
            other.Number == Number;

        public override bool Equals(object? obj) => obj is GitHubPullRequestKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Organization, Repository, Number);

        public override string ToString() => $"{Organization}/{Repository}/{Number}";
    }
}