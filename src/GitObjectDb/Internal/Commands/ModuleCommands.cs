using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace GitObjectDb.Internal.Commands;

internal class ModuleCommands
{
    internal const string ModuleFile = ".gitmodules";

    private static readonly Regex _declarationRegEx = new
        (@"\s*\[submodule\s+\""(.*)\""\]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _pathRegEx = new(
        @"\s*path\s+=\s+(.*)\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _urlRegEx = new(
        @"\s*url\s+=\s+(.*)\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex _branchRegEx = new(
        @"\s*branch\s+=\s+(.*)\s*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly Dictionary<string, ModuleDescription> _modules = new();

    internal ModuleCommands(Tree? tree)
        : this(tree?[ModuleFile]?.Target.Peel<Blob>().GetContentStream())
    {
    }

    internal ModuleCommands(Stream? stream)
    {
        ReadFile(stream);
    }

    internal bool HasAnyChange { get; private set; }

    internal ModuleDescription? this[string module]
    {
        get
        {
            return _modules.TryGetValue(module, out var result) ? result : null;
        }

        set
        {
            if (value is null)
            {
                if (_modules.TryGetValue(module, out var _))
                {
                    HasAnyChange = true;
                }
                _modules.Remove(module);
            }
            else
            {
                if (_modules.TryGetValue(module, out var existing) && existing.Equals(value))
                {
                    return;
                }
                _modules[module] = value;
                HasAnyChange = true;
            }
        }
    }

    private void ReadFile(Stream? stream)
    {
        if (stream is null)
        {
            return;
        }
        using var reader = new StreamReader(stream);
        string? line, module = null, path = null, url = null, branch = null;
        while ((line = reader.ReadLine()) is not null)
        {
            string? match = null;
            if (TryExtractLineData(line, _declarationRegEx, ref match))
            {
                AddModuleIfValidData(ref module, ref path, ref url, ref branch);
                module = match;
            }
            else if (TryExtractLineData(line, _pathRegEx, ref path) ||
                     TryExtractLineData(line, _urlRegEx, ref url) ||
                     TryExtractLineData(line, _branchRegEx, ref branch))
            {
                // Execute above calls to extract first matching data type (path or url or...)
            }
        }
        AddModuleIfValidData(ref module, ref path, ref url, ref branch);
    }

    private static bool TryExtractLineData(string line, Regex regex, ref string? data)
    {
        var match = regex.Match(line);
        if (match.Success)
        {
            data = match.Result("$1");
            return true;
        }
        return false;
    }

    private void AddModuleIfValidData(ref string? module, ref string? path, ref string? url, ref string? branch)
    {
        if (module is not null && path is not null && url is not null)
        {
            _modules[module] = new(path, url, branch);
        }
        module = path = url = branch = null;
    }

    public Stream CreateStream()
    {
        var result = new MemoryStream();
        using var writer = new StreamWriter(result, Encoding.UTF8, 1024, leaveOpen: true)
        {
            NewLine = "\n",
        };
        foreach (var module in _modules)
        {
            writer.WriteLine($"[submodule \"{module.Key}\"]");
            writer.WriteLine($"\tpath = {module.Value.Path}");
            writer.WriteLine($"\turl = {module.Value.Url}");
            if (!string.IsNullOrEmpty(module.Value.Branch))
            {
                writer.WriteLine($"\tbranch = newBranch");
            }
            writer.WriteLine();
        }
        writer.Flush();
        result.Position = 0L;
        return result;
    }

    internal void Remove(string name)
    {
        if (_modules.Remove(name))
        {
            HasAnyChange = true;
        }
    }

    internal void RemoveRecursively(DataPath path)
    {
        if (path.IsNode && path.UseNodeFolders)
        {
            var toRemove = _modules.Keys
                .Where(m => m.StartsWith(path.FolderPath, StringComparison.OrdinalIgnoreCase))
                .ToList();
            foreach (var key in toRemove)
            {
                Remove(key);
            }
        }
    }

    internal record ModuleDescription
    {
        public ModuleDescription(string path, string url, string? branch)
        {
            Path = path;
            Url = url;
            Branch = branch;
        }

        public string Path { get; }

        public string Url { get; }

        public string? Branch { get; }
    }
}
