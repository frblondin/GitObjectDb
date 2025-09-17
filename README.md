**GitObjectDb simplifies the configuration management versioning by backing it in Git.**

| Name | Badge |
| --- | --- |
| GitObjectDb  | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb)](https://www.nuget.org/packages/GitObjectDb/) |
| GitObjectDb.SystemTextJson | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.SystemTextJson)](https://www.nuget.org/packages/GitObjectDb.SystemTextJson/) |
| GitObjectDb.YamlDotNet | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.YamlDotNet)](https://www.nuget.org/packages/GitObjectDb.YamlDotNet/) |
| GitObjectDb.Api.OData | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.Api.OData)](https://www.nuget.org/packages/GitObjectDb.Api.OData/) |
| GitObjectDb.Api.GraphQL | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.Api.GraphQL)](https://www.nuget.org/packages/GitObjectDb.Api.GraphQL/) |
| GitObjectDb.Api.ProtoBuf | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.Api.ProtoBuf)](https://www.nuget.org/packages/GitObjectDb.Api.ProtoBuf/) |
| GitObjectDb.Api.ProtoBuf.Model | [![NuGet Badge](https://img.shields.io/nuget/vpre/GitObjectDb.Api.ProtoBuf.Model)](https://www.nuget.org/packages/GitObjectDb.Api.ProtoBuf.Model/) |

[![Build Status](https://github.com/frblondin/GitObjectDb/actions/workflows/CI.yml/badge.svg)](https://github.com/frblondin/GitObjectDb/actions/workflows/Release.yml)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=alert_status)](https://sonarcloud.io/dashboard/index/GitObjectDb)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=bugs)](https://sonarcloud.io/project/issues?id=GitObjectDb&resolved=false&types=BUG)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=coverage)](https://sonarcloud.io/component_measures?id=GitObjectDb&metric=Coverage)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=code_smells)](https://sonarcloud.io/project/issues?id=GitObjectDb&resolved=false&types=CODE_SMELL)

# Overview

GitObjectDb is designed to simplify the configuration management versioning. It does so by removing the need for hand-coding the commands needed to interact with Git.

The Git repository is used as a pure database as the files containing the serialized copy of the objects are never fetched in the filesystem. GitObjectDb only uses the blob storage provided by Git.

Here's a simple example:
1. Define your own repository data model:
    ```csharp
    [GitFolder("Applications")]
    public record Application : Node
    {
        public string Name { get; init; }

        public string Description { get; init; }
    }
    [GitFolder("Pages")]
    public record Table : Node
    {
        public string Name { get; init; }

        public string Description { get; init; }

        [StoreAsSeparateFile(Extension = "txt")]
        public string? RichContent { get; init; }
    }
    ```
2. Manipulate objects as follows:
    ```csharp
	var existingApplication = await connection.LookupAsync<Application>("main", "applications", new UniqueId(id));
	var newTable = new Table { ... };
	var updates = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(newTable, parent: existingApplication));
	await updates.CommitAsync(new("Added new table.", author, committer));
    ```

# Features

## Structured & unstructured data storage

```csharp
var node = new SomeNode
{
    SomeProperty = "Value stored as json",
    RichContent = "Value stored as raw text in separate Git blob, next to primary one",
}:
```
... gets stored in Git as follows:
* zerzrzrz.json
```json
{
  "$type": "Sample.SomeNode",
  "id": "zerzrzrz",
  "someProperty": "Value stored as json"
}
```
* zerzrzrz.RichContent.txt
```text
Value stored many dynamic resources in separate Git blob, next to primary one
```
You can also store resources as separate files:
```csharp
new Resource(node, "Some/Folder", "File.txt", new Resource.Data("Value stored in a separate file in <node path>/Resources/Some/Folder/File.txt"));
```

## Branching

```csharp
connection.Branches.Add("newBranch", "main~1");
var updates = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(table with { Name = newName }));
await updates..CommitAsync(new("Another message", signature, signature));
```

## Comparing commits

```csharp
var comparison = await connection.CompareAsync("main~5", "main");
var nodeChanges = comparison.Modified.OfType<Change.NodeChange>();
```

## Node references

Node references allows linking existing nodes in a repository:

```csharp
public record Order : Node
{
    public Client Client { get; set; }
    // ...
}
public record Client : Node
{
    // ...
}
// Nodes get loaded with their references (using a shared )
var cache = new Dictionary<DataPath, ITreeItem>();
var order = (await connection.GetNodesAsync<Order>("main", referenceCache: cache)).First();
Console.WriteLine(order.Client.Id);
```

## Merge, Rebase, Cherry-pick

```csharp
// main:      A---B    A---B
//             \    ->  \   \
// newBranch:   C        C---x

var mainChanges = await connection.Update("main", c => c.CreateOrUpdateAsync(table with { Description = newDescription }));
await mainChanges.CommitAsync(new("B", signature, signature));
connection.Repository.Branches.Add("newBranch", "main~1");
var newBranchChanges = await connection.UpdateAsync("newBranch", c => c.CreateOrUpdateAsync(table with { Name = newName }));
await newBranchChanges.CommitAsync(new("C", signature, signature));

var merge = await sut.MergeAsync(branchName: "newBranch", upstreamCommittish: "main");
if (merge.Status = MergeStatus.Conflicts)
{
    // ...
}
```

## Node versioning management

Imagine a scenario where you define in your code a first type:
```csharp
[GitFolder(FolderName = "Items", UseNodeFolders = false)]
[IsDeprecatedNodeType(typeof(SomeNodeV2))]
private record SomeNodeV1 : Node
{
    public int Flags { get; set; }
}

[GitFolder(FolderName = "Items", UseNodeFolders = false)]
private record SomeNodeV2 : Node
{
    public BindingFlags TypedFlags { get; set; }
}
```
You then want to introduce a new change so that the `Flags` property contains more meaningful information, relying on enums:
```csharp
[GitFolder(FolderName = "Items", UseNodeFolders = false)]
private record SomeNodeV2 : Node
{
    public BindingFlags TypedFlags { get; set; }
}
```
All you need to do is to #1 add the `[IsDeprecatedNodeType(typeof(SomeNodeV2))]` attribute. This will instruct the deserializer to convert nodes to new version, using a converter. #2 converter needs to be provided in the model. You can use AutoMapper or other tools at your convenience.
```csharp
[GitFolder(FolderName = "Items", UseNodeFolders = false)]
[IsDeprecatedNodeType(typeof(SomeNodeV2))]
private record SomeNodeV1 : Node
{
    // ...
}
var model = new ConventionBaseModelBuilder()
    .RegisterType<SomeNodeV1>()
    .RegisterType<SomeNodeV2>()
    .AddDeprecatedNodeUpdater(UpdateDeprecatedNode)
    .Build();
Node UpdateDeprecatedNode(Node old, Type targetType)
{
    var nodeV1 = (SomeNodeV1)old;
    return new SomeNodeV2
    {
        Id = old.Id,
        TypedFlags = (BindingFlags)nodeV1.Flags,
    };
}
```


# Documentation

See [documentation][Documentation].

 [Documentation]: https://gitobjectdb.readthedocs.io

# Prerequisites

 - .NET Standard 2.0 or 2.1

# Online resources

 - [GitDotNet][GitDotNet] (Requires NuGet 2.7+)

 [GitDotNet]: https://github.com/frblondin/GitDotNet

# Quick contributing guide

 - Fork and clone locally
 - Create a topic specific branch. Add some nice feature. Do not forget the tests ;-)
 - Send a Pull Request to spread the fun!

# License

The MIT license (Refer to the [LICENSE][license] file).

 [license]: https://github.com/frblondin/GitObjectDb/blob/master/LICENSE
