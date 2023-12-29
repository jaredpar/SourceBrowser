using GitHubJwt;
using Microsoft.Extensions.Configuration;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.SourceBrowser.SourceIndexServer;

public interface IGitHubClientFactory
{
    Task<IGitHubClient> CreateForAppAsync(string owner, string repository);
}

public sealed class GitHubClientFactory : IGitHubClientFactory
{
    public const string GitHubProductName = "codenav.azurewebsites.net";

    private string AppPrivateKey { get; }
    public int AppId { get; }

    public GitHubClientFactory(IConfiguration configuration)
    {
        AppId = int.Parse(configuration[Constants.KeyGitHubAppId]);
        AppPrivateKey = configuration[Constants.KeyGitHubAppSecretKey];
    }

    public GitHubClientFactory(int appId, string privateKey)
    {
        AppId = appId;
        AppPrivateKey = privateKey;
    }

    public async Task<GitHubClient> CreateForAppAsync(string owner, string repository)
    {
        var gitHubClient = CreateForAppCore();
        var installation = await gitHubClient.GitHubApps.GetRepositoryInstallationForCurrent(owner, repository).ConfigureAwait(false);
        var installationToken = await gitHubClient.GitHubApps.CreateInstallationToken(installation.Id);
        return CreateForToken(installationToken.Token, AuthenticationType.Oauth);
    }

    private GitHubClient CreateForAppCore()
    {
        // See: https://octokitnet.readthedocs.io/en/latest/github-apps/ for details.

        var privateKeySource = new PlainStringPrivateKeySource(AppPrivateKey);
        var generator = new GitHubJwtFactory(
            privateKeySource,
            new GitHubJwtFactoryOptions
            {
                AppIntegrationId = AppId,
                ExpirationSeconds = 600                    
            });

        var token = generator.CreateEncodedJwtToken();

        return CreateForToken(token, AuthenticationType.Bearer);
    }

    public static GitHubClient CreateForToken(string token, AuthenticationType authenticationType)
    {
        var productInformation = new ProductHeaderValue(GitHubProductName);
        var client = new GitHubClient(productInformation)
        {
            Credentials = new Credentials(token, authenticationType)
        };
        return client;
    }

    #region IGitHubClientFactory

    async Task<IGitHubClient> IGitHubClientFactory.CreateForAppAsync(string owner, string repository) =>
        await CreateForAppAsync(owner, repository).ConfigureAwait(false);

    #endregion

    private sealed class PlainStringPrivateKeySource : IPrivateKeySource
    {
        private readonly string _key;

        internal PlainStringPrivateKeySource(string key)
        {
            _key = key;
        }

        public TextReader GetPrivateKeyReader() => new StringReader(_key);
    }
}
