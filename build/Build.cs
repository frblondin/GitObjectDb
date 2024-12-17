using Nuke.Common;
using Nuke.Common.ChangeLog;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.Git;
using Nuke.Common.Tools.GitHub;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Tools.SonarScanner;
using Nuke.Common.Utilities.Collections;
using Octokit;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using Nuke.Common.CI.GitHubActions.Configuration;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tooling.ProcessTasks;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;
using static System.Net.WebRequestMethods;
using static DotNetCollectTasks;
using Project = Nuke.Common.ProjectModel.Project;

[ShutdownDotNetAfterServerBuild]
[GitHubActionsCustom(
    "CI",
    GitHubActionsImage.UbuntuLatest,
    JavaDistribution = GitHubActionJavaDistribution.Temurin,
    JavaVersion = "17",
    OnPushBranches =
    [
        "main",
        "dev",
        "releases/**",
    ],
    OnPullRequestBranches =
    [
        "main",
        "releases/**",
    ],
    
    InvokedTargets = [nameof(Pack)],
    ImportSecrets = ["GITHUB_TOKEN", "SONAR_TOKEN", nameof(NuGetApiKey)],
    FetchDepth = 0)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Pack);

    [NerdbankGitVersioning(UpdateBuildNumber = true)] readonly NerdbankGitVersioning GitVersion;
    [GitRepository] readonly GitRepository Repository;
    [Solution(SuppressBuildProjectCheck = true)] readonly Solution Solution;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server).")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    static AbsolutePath SourceDirectory => RootDirectory / "src";
    static AbsolutePath PackagePropFile => SourceDirectory / "Directory.Packages.props";
    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath NerdbankGitVersioningDirectory => OutputDirectory / "NerdbankGitVersioning";
    static AbsolutePath TestDirectory => OutputDirectory / "tests";
    static AbsolutePath CoverageResult => OutputDirectory / "coverage";
    static AbsolutePath NugetDirectory => OutputDirectory / "nuget";
    static AbsolutePath ChangeLogFile => RootDirectory / "CHANGELOG.md";

    [Parameter(Name = "GITHUB_HEAD_REF")] readonly string GitHubHeadRef;

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
    [Parameter, Secret] readonly string SonarToken;

    string GitHubNugetFeed => GitHubActions.Instance != null
        ? $"https://nuget.pkg.github.com/{GitHubActions.Instance.RepositoryOwner}/index.json"
        : null;
    [Parameter] string NuGetFeed { get; } = "https://api.nuget.org/v3/index.json";
    [Parameter, Secret] readonly string NuGetApiKey;

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(d => d.DeleteDirectory());
            OutputDirectory.CreateOrCleanDirectory();
        });

    Target StartSonarqube => _ => _
        .DependsOn(Clean)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarToken))
        .WhenSkipped(DependencyBehavior.Execute)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            SonarScannerTasks.SonarScannerBegin(settings =>
            {
                settings = settings
                    .SetServer(SonarHostUrl)
                    .SetOrganization(SonarOrganization)
                    .SetToken(SonarToken)
                    .SetName(SonarqubeProjectKey)
                    .SetProjectKey(SonarqubeProjectKey)
                    .SetVersion(GitVersion.AssemblyVersion)
                    .EnableExcludeTestProjects()
                    .AddAdditionalParameter("sonar.scanner.scanAll", "false")
                    .AddVisualStudioCoveragePaths(CoverageResult / "coverage.xml")
                    .AddCoverageExclusions("**/tests/**/*.*, **/samples/**/*.*")
                    .AddSourceExclusions("**/tests/**, **/samples/**")
                    .SetVSTestReports(TestDirectory / "*.trx");

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
            Collect(new DotNetCollectSettings()
                .SetProcessToolPath(NuGetToolPathResolver.GetPackageExecutable("dotnet-coverage", "dotnet-coverage.dll"))
                .SetTarget(new DotNetTestSettings()
                    .SetProjectFile(Solution)
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetLoggers("trx")
                    .AddProcessAdditionalArguments("-m:1") // Make sure only one assembly gets tested at a time for coverage collect
                    .SetResultsDirectory(TestDirectory))
                .SetConfigFile(SourceDirectory / "CoverageConfig.xml")
                .SetFormat("xml")
                .SetOutput(CoverageResult / "coverage.xml"));
        });

    Target CoverageReport => _ => _
        .DependsOn(Test)
        .AssuredAfterFailure()
        .OnlyWhenStatic(() => IsLocalBuild)
        .WhenSkipped(DependencyBehavior.Execute)
        .Executes(() =>
        {
            ReportGenerator(s => s
                .SetReports(CoverageResult / "coverage.xml")
                .SetTargetDirectory(CoverageResult));
        });

    Target EndSonarqube => _ => _
        .DependsOn(CoverageReport)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarToken))
        .WhenSkipped(DependencyBehavior.Execute)
        .Executes(() =>
        {
            SonarScannerTasks.SonarScannerEnd(c => c
                .SetToken(SonarToken));
        });

    Target Pack => _ => _
        .DependsOn(EndSonarqube)
        .Produces(NugetDirectory / ArtifactsType)
        .Triggers(PublishToGithub, PublishToNuGet, CreateRelease)
        .Executes(() =>
        {
            var modifiedFilesSinceLastTag = GitChangeLogTasks.ChangedFilesSinceLastTag();
            var modifiedPackages =
                (from line in GitChangeLogTasks.GetModifiedLinesSinceLastTag(PackagePropFile)
                 let match = Regex.Match(line, @"<PackageVersion\s+[^>]*Include\s*=\s*""([^""]*)""")
                 where match.Success
                 select match.Groups[1].Value).ToList();

            Solution.AllProjects
                .Where(p => p.GetProperty<string>("PackageType") == "Dependency")
                .Where(HasProjectBeenModifiedSinceLastTag)
                .ForEach(project =>
                    DotNetPack(s => s
                        .SetProject(project)
                        .SetConfiguration(Configuration)
                        .SetVersion(GitVersion.NuGetPackageVersion)
                        .EnableNoBuild()
                        .EnableNoRestore()
                        .SetOutputDirectory(NugetDirectory)));

            bool HasProjectBeenModifiedSinceLastTag(Project project)
            {
                var gitPath = project.Path.ToGitPath(RootDirectory);
                var projectContent = System.IO.File.ReadAllText(project.Path);
                return modifiedFilesSinceLastTag.Any(f => f.StartsWith(gitPath)) ||
                       modifiedPackages.Any(package => projectContent.Contains(package));
            }
        });

    Target PublishToGithub => _ => _
       .Description($"Publishing to GitHub for Development only.")
       .Requires(() => Configuration.Equals(Configuration.Release))
       .OnlyWhenStatic(() => GitHubActions.Instance != null &&
                             GitHubHeadRef != null &&
                             (GitHubHeadRef.StartsWith("dev") || GitHubHeadRef.StartsWith("feature")))
       .Executes(() =>
       {
           NugetDirectory.GlobFiles(ArtifactsType)
               .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
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
       .OnlyWhenStatic(() => GitHubActions.Instance != null &&
                             Repository.IsOnMainOrMasterBranch())
       .Executes(() =>
       {
           NugetDirectory.GlobFiles(ArtifactsType)
               .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
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
       .OnlyWhenStatic(() => GitHubActions.Instance != null &&
                             Repository.IsOnMainOrMasterBranch())
       .Executes(async () =>
       {
           GitHubTasks.GitHubClient = new GitHubClient(
               new ProductHeaderValue(nameof(NukeBuild)),
               new Octokit.Internal.InMemoryCredentialStore(
                   new Credentials(GitHubActions.Instance.Token)));

           var (owner, name) = (Repository.GetGitHubOwner(), Repository.GetGitHubName());

           var releaseTag = GitVersion.NuGetPackageVersion;
           var messages = GitChangeLogTasks.CommitsSinceLastTag();
           var latestChangeLog = string.Join("\n", messages.Where(IsReleaseNoteCommit).Select(TurnIntoLog));

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

           NugetDirectory.GlobFiles(ArtifactsType)
              .Where(x => !x.Name.EndsWith(ExcludedArtifactsType))
              .ForEach(async x => await UploadReleaseAssetToGitHub(createdRelease, x));

           await GitHubTasks.GitHubClient
              .Repository.Release
              .Edit(owner, name, createdRelease.Id, new ReleaseUpdate { Draft = false });

           static bool IsReleaseNoteCommit(string message) =>
               !message.Contains("[skip release notes]", StringComparison.OrdinalIgnoreCase);

           static string TurnIntoLog(string message) =>
               $"- {Regex.Replace(message, @"\s*\[.*\]", string.Empty)}";
       });


    private static async Task UploadReleaseAssetToGitHub(Release release, string asset)
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
