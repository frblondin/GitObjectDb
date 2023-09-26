using System.Collections.Generic;
using System.Linq;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.CI.GitHubActions.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Utilities;

public class GitHubActionsCustomAttribute : GitHubActionsAttribute
{
    public required GitHubActionJavaDistribution JavaDistribution { get; init; }
    public required string JavaVersion { get; init; }

    public GitHubActionsCustomAttribute(string name, GitHubActionsImage image, params GitHubActionsImage[] images)
        : base(name, image, images)
    {
    }

    protected override GitHubActionsJob GetJobs(GitHubActionsImage image, IReadOnlyCollection<ExecutableTarget> relevantTargets)
    {
        var result = base.GetJobs(image, relevantTargets);
        result.Steps = result.Steps
            .Prepend(new GitHubActionsSetupJavaStep
            {
                Distribution = JavaDistribution,
                Version = JavaVersion,
            }).ToArray();

        return result;
    }
}

public class GitHubActionsSetupJavaStep : GitHubActionsStep
{
    public GitHubActionJavaDistribution Distribution { get; set; }
    public string Version { get; set; }

    public override void Write(CustomFileWriter writer)
    {
        writer.WriteLine("- uses: actions/setup-java@v3");

        using (writer.Indent())
        {
            writer.WriteLine("with:");
            using (writer.Indent())
            {
                writer.WriteLine($"distribution: '{Distribution!.ToString().ToLowerInvariant()}'");
                writer.WriteLine($"java-version: '{Version}'");
            }
        }
    }
}

public enum GitHubActionJavaDistribution
{
    Temurin,
    Zulu,
    Adopt,
    Liberica,
    Microsoft,
    Corretto,
    Semeru,
    Oracle,
    Dragonwell,
}