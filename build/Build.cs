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

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions(
    "Release",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Default) },
    ImportSecrets = new[] { "SONAR_TOKEN" })]
[GitHubActions(
    "PullRequestValidation",
    GitHubActionsImage.UbuntuLatest,
    OnPullRequestBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Run) },
    ImportSecrets = new[] { "SONAR_TOKEN" })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Default);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server).")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter(".Net Framework version.")]
    readonly string NetFramework = "net6.0";

    [Solution] readonly Solution Solution;

    static AbsolutePath SourceDirectory => RootDirectory / "src";

    static AbsolutePath OutputDirectory => RootDirectory / "output";
    static AbsolutePath NerdbankGitVersioningDirectory => OutputDirectory / "NerdbankGitVersioning";
    static AbsolutePath TestDirectory => OutputDirectory / "tests";
    static AbsolutePath CoverageResult => OutputDirectory / "coverage";
    static AbsolutePath NugetDirectory => OutputDirectory / "nuget";

    [Parameter] readonly string PrNumber;
    [Parameter] readonly string PrTargetBranch;
    [Parameter] readonly string BuildBranch;
    [Parameter] readonly string BuildNumber;

    bool IsPR => !string.IsNullOrEmpty(PrNumber);

    [Parameter] string SonarHostUrl { get; } = "https://sonarcloud.io";
    [Parameter] string SonarOrganization { get; } = "frblondin-github";
    [Parameter] string SonarqubeProjectKey { get; } = "GitObjectDb";
    [Parameter(Name = "SONAR_TOKEN")] readonly string SonarLogin;

    string gitVersion;

    Target Run => _ => _
        .DependsOn(Clean)
        .DependsOn(GitVersion)
        .DependsOn(StartSonarqube)
        .DependsOn(Restore)
        .DependsOn(Compile)
        .DependsOn(Test)
        .DependsOn(Coverage)
        .DependsOn(EndSonarqube);

    Target Default => _ => _
        .DependsOn(Run)
        .DependsOn(Pack);

    Target Clean => _ => _
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target GitVersion => _ => _
        .After(Clean)
        .Executes(() =>
        {
            var toolPath = ToolPathResolver.GetPackageExecutable("nbgv", "nbgv.dll");
            var process = StartProcess(toolPath, "get-version", SourceDirectory)
                .AssertZeroExitCode();
            var rawVersion = process.Output.First().Text;
            gitVersion = Regex.Replace(rawVersion, @"Version:\s+(.*)", "$1");
        });

    Target Restore => _ => _
        .After(GitVersion)
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .After(Restore)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoRestore());
        });

    Target Test => _ => _
        .After(Compile)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .EnableNoBuild()
                .EnableNoRestore()
                .SetLoggers("trx")
                .SetProcessArgumentConfigurator(arguments => arguments
                    .Add("/p:CollectCoverage=true")
                    .Add("/p:Exclude=\\\"{0}\\\"", "[*Tests]*,[Models.Software]*,[MetadataStorageConverter]*")
                    .Add("/p:CoverletOutput={0}/", CoverageResult)
                    .Add("/p:CoverletOutputFormat=\\\"{0}\\\"", "opencover,json")
                    .Add("/p:UseSourceLink={0}", "true")
                    .Add("/p:MergeWith={0}", CoverageResult / "coverage.json")
                    .Add("-m:1"))
                .SetResultsDirectory(TestDirectory));
        });

    Target Coverage => _ => _
        .After(Test)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            ReportGenerator(s => s
                .SetFramework(NetFramework)
                .SetReports(CoverageResult / "coverage.opencover.xml")
                .SetTargetDirectory(CoverageResult));
        });

    Target StartSonarqube => _ => _
        .Before(Compile)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarLogin))
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
                    .SetVersion($"{gitVersion}.build-{BuildNumber}")
                    .AddOpenCoverPaths(CoverageResult / "coverage.opencover.xml")
                    .AddCoverageExclusions("**/*.Tests/**/*.*, **/GitObjectDb.Web/**/*.*, **/MetadataStorageConverter*/**/*.*, **/Models.Software/**/*.*")
                    .AddSourceExclusions("**/MetadataStorageConverter*/**, **/Models.Software/**")
                    .SetVSTestReports(TestDirectory);

                return IsPR ?
                    settings.SetPullRequestBase(PrTargetBranch)
                            .SetPullRequestBranch(BuildBranch)
                            .SetPullRequestKey(PrNumber):
                    settings.SetBranchName(BuildBranch);
            });
        });

    Target EndSonarqube => _ => _
        .After(Test)
        .OnlyWhenStatic(() => !string.IsNullOrWhiteSpace(SonarLogin))
        .Executes(() =>
        {
            SonarScannerTasks.SonarScannerEnd(c => c
                .SetLogin(SonarLogin)
                .SetFramework("net5.0"));
        });

    Target Pack => _ => _
        .After(EndSonarqube)
        .Produces(NugetDirectory / "*.nupkg")
        .Executes(() =>
        {
            foreach (var project in new[] { "GitObjectDb", "GitObjectDb.Api", "GitObjectDb.Api.GraphQL", "GitObjectDb.Api.OData" })
            {
                DotNetPack(s => s
                    .SetProject(Solution.GetProject(project))
                    .SetConfiguration(Configuration)
                    .EnableNoBuild()
                    .EnableNoRestore()
                    .SetOutputDirectory(OutputDirectory / "nuget"));
            }
        });
}
