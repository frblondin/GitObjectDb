using JetBrains.Annotations;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using System.Collections.Generic;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using System.Linq;
using System.Collections.Immutable;

[PublicAPI]
[ExcludeFromCodeCoverage]
[Command(Type = typeof(DotNetCollectTasks), Command = nameof(DotNetCollectTasks.Collect), Arguments = "collect")]
public partial class DotNetCollectSettings : ToolOptions
{
    [Argument(Format = "{value}", Position = 1)] public IReadOnlyList<string> Target => Get<IReadOnlyList<string>>(() => Target);
    [Argument(Format = "-s {value}")] public string ConfigFile => Get<string>(() => ConfigFile);
    [Argument(Format = "-f {value}")] public string Format => Get<string>(() => Format);
    [Argument(Format = "-o {value}")] public string Output => Get<string>(() => Output);
}

public static class DotNetCollectSettingsExtensions
{
    [Pure]
    public static T SetTarget<T>(this T o, DotNetTestSettings v) where T : DotNetCollectSettings => o.Modify(b =>
    {
        var arguments = ReflectionUtility.InvokeMember<IEnumerable<string>>(typeof(ToolOptions), "GetArguments", v, BindingFlags.Instance | BindingFlags.NonPublic);
        b.Set(() => o.Target, arguments.Prepend(DotNetTasks.DotNetPath).ToImmutableList());
    });
    [Pure]
    public static T SetConfigFile<T>(this T o, string v) where T : DotNetCollectSettings => o.Modify(b => b.Set(() => o.ConfigFile, v));
    [Pure]
    public static T SetFormat<T>(this T o, string v) where T : DotNetCollectSettings => o.Modify(b => b.Set(() => o.Format, v));
    [Pure]
    public static T SetOutput<T>(this T o, string v) where T : DotNetCollectSettings => o.Modify(b => b.Set(() => o.Output, v));
}

[PublicAPI]
[ExcludeFromCodeCoverage]
public partial class DotNetCollectTasks : ToolTasks, IRequirePathTool
{
    public static IReadOnlyCollection<Output> Collect(DotNetCollectSettings options = null) => new DotNetCollectTasks().Run<DotNetCollectSettings>(options);
}