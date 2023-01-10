using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tooling.ProcessTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using System.Linq;
using System.Diagnostics;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;
using Nuke.Common.Tools.GitHub;
using Octokit;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.ChangeLog;
using System;
using System.Threading.Tasks;
using Nuke.Common.Tools.NerdbankGitVersioning;

[ShutdownDotNetAfterServerBuild]
[GitHubActions(
    "CI",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[]
    {
        "main",
        "dev",
        "releases/**",
    },
    OnPullRequestBranches = new[]
    {
        "main",
        "releases/**",
    },
    InvokedTargets = new[] { nameof(Pack) },
    ImportSecrets = new[] { "GITHUB_TOKEN", "SONAR_TOKEN", nameof(NuGetApiKey) },
    FetchDepth = 0)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Pack);

    const string NetFramework = "net7.0";

    [NerdbankGitVersioning(UpdateBuildNumber = true)] readonly NerdbankGitVersioning GitVersion;
    [GitRepository] readonly GitRepository Repository;
    [Solution(SuppressBuildProjectCheck = true)] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server).")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath NerdbankGitVersioningDirectory => OutputDirectory / "NerdbankGitVersioning";
    static AbsolutePath TestDirectory => OutputDirectory / "tests";
    static AbsolutePath CoverageResult => OutputDirectory / "coverage";
    static AbsolutePath NugetDirectory => OutputDirectory / "nuget";
    static AbsolutePath ChangeLogFile => RootDirectory / "CHANGELOG.md";

    [Parameter] string ArtifactsType { get; } = "*.nupkg";
    [Parameter] string ExcludedArtifactsType { get; } = "symbols.nupkg";

    [Parameter] readonly string PrNumber;
    [Parameter] readonly string PrTargetBranch;
    [Parameter] readonly string BuildBranch;
    [Parameter] readonly string BuildNumber;

    bool IsPR => !string.IsNullOrEmpty(PrNumber);

    [Parameter] string SonarHostUrl { get; } = "https://sonarcloud.io";
    [Parameter] string SonarOrganization { get; } = "frblondin-github";
    [Parameter] string SonarqubeProjectKey { get; } = "GitObjectDb";
    [Parameter(Name = "SONAR_TOKEN")] readonly string SonarLogin;

    string GitHubNugetFeed => GitHubActions.Instance != null
        ? $"https://nuget.pkg.github.com/{GitHubActions.Instance.RepositoryOwner}/index.json"
        : null;
    [Parameter] string NuGetFeed { get; } = "https://api.nuget.org/v3/index.json";
    [Parameter, Secret] readonly string NuGetApiKey;

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target StartSonarqube => _ => _
        .DependsOn(Clean)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarLogin))
        .WhenSkipped(DependencyBehavior.Execute)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SonarScannerTasks.SonarScannerBegin(settings =>
            {
                settings = settings
                    .SetServer(SonarHostUrl)
                    .SetOrganization(SonarOrganization)
                    .SetLogin(SonarLogin)
                    .SetFramework("net5.0")
                    .SetName(SonarqubeProjectKey)
                    .SetProjectKey(SonarqubeProjectKey)
                    .SetVersion(GitVersion.AssemblyVersion)
                    .AddOpenCoverPaths(CoverageResult / "coverage.opencover.xml")
                    .AddCoverageExclusions("**/*.Tests/**/*.*, **/GitObjectDb.Web/**/*.*, **/MetadataStorageConverter*/**/*.*, **/Models.Software/**/*.*")
                    .AddSourceExclusions("**/MetadataStorageConverter*/**, **/Models.Software/**")
                    .SetVSTestReports(TestDirectory);

                return IsPR ?
                    settings.SetPullRequestBase(PrTargetBranch)
                            .SetPullRequestBranch(BuildBranch)
                            .SetPullRequestKey(PrNumber) :
                    settings.SetBranchName(BuildBranch);
            });
        });

    Target Restore => _ => _
        .DependsOn(StartSonarqube)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .DependsOn(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetLoggers("trx")                
                .SetResultsDirectory(TestDirectory));
        });

    Target Coverage => _ => _
        .DependsOn(Test)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            ReportGenerator(s => s
                .SetFramework("net6.0")
                .SetReports(CoverageResult / "coverage.opencover.xml")
                .SetTargetDirectory(CoverageResult));
        });

    Target EndSonarqube => _ => _
        .DependsOn(Coverage)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarLogin))
        .WhenSkipped(DependencyBehavior.Execute)
        .Executes(() =>
        {
            SonarScannerTasks.SonarScannerEnd(c => c
                .SetLogin(SonarLogin)
                .SetFramework("net5.0"));
        });

    Target Pack => _ => _
        .DependsOn(EndSonarqube)
        .Produces(NugetDirectory / ArtifactsType)
        .Triggers(PublishToGithub, PublishToNuGet)
        .Executes(() =>
        {
            Solution.AllProjects
                .Where(p => p.GetProperty<string>("PackageType") == "Dependency")
                .ForEach(project =>
                    DotNetPack(s => s
                        .SetProject(Solution.GetProject(project))
                        .SetConfiguration(Configuration)
                        .SetVersion(GitVersion.NuGetPackageVersion)
                        .SetIncludeSource(true)
                        .SetIncludeSymbols(true)
                        .EnableNoBuild()
                        .EnableNoRestore()
                        .SetOutputDirectory(NugetDirectory)));
        });

    Target PublishToGithub => _ => _
       .Description($"Publishing to Github for Development only.")
       .Triggers(CreateRelease)
       .Requires(() => Configuration.Equals(Configuration.Release))
       .OnlyWhenStatic(() => GitHubActions.Instance != null && string.IsNullOrEmpty(GitHubActions.Instance.HeadRef) && // Not from a fork
                             (Repository.IsOnDevelopBranch() || GitHubActions.Instance.IsPullRequest))
       .Executes(() =>
       {
           GlobFiles(NugetDirectory, ArtifactsType)
               .Where(x => !x.EndsWith(ExcludedArtifactsType))
               .ForEach(x =>
               {
                   DotNetNuGetPush(s => s
                       .SetTargetPath(x)
                       .SetSource(GitHubNugetFeed)
                       .SetApiKey(GitHubActions.Instance.Token)
                       .EnableSkipDuplicate()
                   );
               });
       });
    
    Target PublishToNuGet => _ => _
       .Description($"Publishing to NuGet with the version.")
       .Requires(() => Configuration.Equals(Configuration.Release))
       .OnlyWhenStatic(() => Repository.IsOnMainOrMasterBranch())
       .Executes(() =>
       {
           GlobFiles(NugetDirectory, ArtifactsType)
               .Where(x => !x.EndsWith(ExcludedArtifactsType))
               .ForEach(x =>
               {
                   DotNetNuGetPush(s => s
                       .SetTargetPath(x)
                       .SetSource(NuGetFeed)
                       .SetApiKey(NuGetApiKey)
                       .EnableSkipDuplicate()
                   );
               });
       });

    Target CreateRelease => _ => _
       .Description($"Creating release for the publishable version.")
       .Requires(() => Configuration.Equals(Configuration.Release))
       .OnlyWhenStatic(() => GitHubActions.Instance != null && (Repository.IsOnMainOrMasterBranch() || Repository.IsOnReleaseBranch()))
       .Executes(async () =>
       {
           GitHubTasks.GitHubClient = new GitHubClient(
               new ProductHeaderValue(nameof(NukeBuild)),
               new Octokit.Internal.InMemoryCredentialStore(
                   new Credentials(GitHubActions.Instance.Token)));

           var (owner, name) = (Repository.GetGitHubOwner(), Repository.GetGitHubName());

           var releaseTag = GitVersion.NuGetPackageVersion;
           var changeLogSectionEntries = ChangelogTasks.ExtractChangelogSectionNotes(ChangeLogFile);
           var latestChangeLog = changeLogSectionEntries
               .Aggregate((c, n) => c + Environment.NewLine + n);

           var newRelease = new NewRelease(releaseTag)
           {
               TargetCommitish = Repository.Commit,
               Draft = true,
               Name = $"v{releaseTag}",
               Prerelease = !(Repository.IsOnMainOrMasterBranch() || Repository.IsOnReleaseBranch()),
               Body = latestChangeLog
           };

           var createdRelease = await GitHubTasks.GitHubClient
              .Repository
              .Release.Create(owner, name, newRelease);

           GlobFiles(NugetDirectory, ArtifactsType)
              .Where(x => !x.EndsWith(ExcludedArtifactsType))
              .ForEach(async x => await UploadReleaseAssetToGithub(createdRelease, x));

           await GitHubTasks.GitHubClient
              .Repository.Release
              .Edit(owner, name, createdRelease.Id, new ReleaseUpdate { Draft = false });
       });


    private static async Task UploadReleaseAssetToGithub(Release release, string asset)
    {
        await using var artifactStream = System.IO.File.OpenRead(asset);
        var fileName = System.IO.Path.GetFileName(asset);
        var assetUpload = new ReleaseAssetUpload
        {
            FileName = fileName,
            ContentType = "application/octet-stream",
            RawData = artifactStream,
        };
        await GitHubTasks.GitHubClient.Repository.Release.UploadAsset(release, assetUpload);
    }
}
