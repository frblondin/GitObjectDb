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
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

[CheckBuildProjectConfigurations]
[ShutdownDotNetAfterServerBuild]
[GitHubActions(
    "ci",
    GitHubActionsImage.UbuntuLatest,
    OnPushBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Coverage), nameof(Pack) })]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main () => Execute<Build>(x => x.Pack);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server).")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Parameter("Coverage threshold. Default is 80%.")]
    readonly int CoverageThreshold = 80;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath OutputDirectory => RootDirectory / "output";
    AbsolutePath TestDirectory => OutputDirectory / "tests";
    AbsolutePath CoverageResult => OutputDirectory / "coverage";
    AbsolutePath NugetDirectory => OutputDirectory / "nuget";

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(OutputDirectory);
        });

    Target Restore => _ => _
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
                .SetProcessArgumentConfigurator(arguments => arguments
                    .Add("/p:CollectCoverage=true")
                    .Add("/p:CoverletOutput={0}/", CoverageResult)
                    .Add("/p:Threshold={0}", CoverageThreshold)
                    .Add("/p:ThresholdType={0}", "line")
                    .Add("/p:UseSourceLink={0}", "true")
                    .Add("/p:CoverletOutputFormat={0}", "cobertura"))
                .SetResultsDirectory(TestDirectory));

            AzurePipelines.Instance?.PublishTestResults(
                "Test results",
                AzurePipelinesTestResultsType.VSTest,
                GlobFiles(TestDirectory.Name, "*.trx"));
        });

    Target Coverage => _ => _
        .DependsOn(Test)
        .AssuredAfterFailure()
        .Executes(() =>
        {
            ReportGenerator(s => s
                .SetFramework("net6.0")
                //.SetProcessToolPath(ToolPathResolver.GetPackageExecutable("dotnet-reportgenerator-globaltool", "ReportGenerator.dll", framework: "net6.0"))
                .SetReports(CoverageResult / "coverage.cobertura.xml")
                .SetVerbosity(ReportGeneratorVerbosity.Verbose)
                .SetTargetDirectory(CoverageResult));

            //AzurePipelines?.UploadArtifact(ArtifactDirectory);

            AzurePipelines.Instance?.PublishCodeCoverage(
                AzurePipelinesCodeCoverageToolType.Cobertura,
                CoverageResult / "coverage.cobertura.xml",
                CoverageResult);
        });

    Target Pack => _ =>
    {
        return _
                .DependsOn(Coverage)
                .Produces(NugetDirectory / "*.nupkg")
                .Executes(() =>
                {
                    foreach (var project in new[] { "GitObjectDb", "GitObjectDb.OData" })
                    {
                        DotNetPack(s => s
                            .SetProject(Solution.GetProject(project))
                            .SetConfiguration(Configuration)
                            .EnableNoBuild()
                            .EnableNoRestore()
                            .SetOutputDirectory(OutputDirectory / "nuget"));
                    }
                });
    };
}
