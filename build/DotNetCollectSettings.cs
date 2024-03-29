using JetBrains.Annotations;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using System;
using System.Diagnostics.CodeAnalysis;

[PublicAPI]
[ExcludeFromCodeCoverage]
[Serializable]
public class DotNetCollectSettings : ToolSettings
{
    /// <summary>
    ///   Path to the DotNet executable.
    /// </summary>
    public override string ProcessToolPath => base.ProcessToolPath ?? DotNetTasks.DotNetPath;
    public string CoveragePath => NuGetToolPathResolver.GetPackageExecutable("dotnet-coverage", "dotnet-coverage.dll");
    public override Action<OutputType, string> ProcessLogger => base.ProcessLogger ?? DotNetTasks.DotNetLogger;
    public override Action<ToolSettings, IProcess> ProcessExitHandler => base.ProcessExitHandler ?? DotNetTasks.DotNetExitHandler;
    public virtual string Target { get; internal set; }
    public virtual AbsolutePath ConfigFile { get; internal set; }
    public virtual string Format { get; internal set; }
    public virtual AbsolutePath Output { get; internal set; }
    protected override Arguments ConfigureProcessArguments(Arguments arguments)
    {
        arguments
          .Add(CoveragePath)
          .Add("collect")
          .Add("{value}", Target)
          .Add("-s {value}", ConfigFile)
          .Add("-f {value}", Format)
          .Add("-o {value}", Output);
        return base.ConfigureProcessArguments(arguments);
    }
}
public static class DotNetCollectSettingsExtensions
{
    [Pure]
    public static T SetTarget<T>(this T toolSettings, string target) where T : DotNetCollectSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Target = target;
        return toolSettings;
    }
    [Pure]
    public static T SetConfigFile<T>(this T toolSettings, AbsolutePath value) where T : DotNetCollectSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.ConfigFile = value;
        return toolSettings;
    }
    [Pure]
    public static T SetFormat<T>(this T toolSettings, string value) where T : DotNetCollectSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Format = value;
        return toolSettings;
    }
    [Pure]
    public static T SetOutput<T>(this T toolSettings, AbsolutePath value) where T : DotNetCollectSettings
    {
        toolSettings = toolSettings.NewInstance();
        toolSettings.Output = value;
        return toolSettings;
    }
}
