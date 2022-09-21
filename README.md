**GitObjectDb simplifies the configuration management versioning by backing it in Git.**

| Name | Badge |
| --- | --- |
| GitObjectDb  | [![NuGet Badge](https://buildstats.info/nuget/GitObjectDb?includePreReleases=true)](https://www.nuget.org/packages/GitObjectDb/) |
| GitObjectDb.Api | [![NuGet Badge](https://buildstats.info/nuget/GitObjectDb.Api?includePreReleases=true)](https://www.nuget.org/packages/GitObjectDb.Api/) |
| GitObjectDb.Api.OData | [![NuGet Badge](https://buildstats.info/nuget/GitObjectDb.Api.OData?includePreReleases=true)](https://www.nuget.org/packages/GitObjectDb.Api.OData/) |
| GitObjectDb.Api.GraphQL | [![NuGet Badge](https://buildstats.info/nuget/GitObjectDb.Api.GraphQL?includePreReleases=true)](https://www.nuget.org/packages/GitObjectDb.Api.GraphQL/) |

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
        public string Name { get; set; }

        public string Description { get; set; }
    }
    [GitFolder("Pages")]
    public record Table : Node
    {
        public string Name { get; set; }

        public string Description { get; set; }
    }
    ```
2. Manipulate objects as follows:
    ```csharp
	var existingApplication = connection.Lookup<Application>("applications", new UniqueId(id));
	var newTable = new Table { ... };
	connection
	    .Update(c => c.CreateOrUpdate(newTable, parent: existingApplication))
		.Commit("Added new table.", author, committer);
    ```

# Features

## Structured & unstructured data storage

```csharp
var node = new SomeNode
{
    SomeProperty = "Value stored as json",
    EmbeddedResource = "Value stored as raw text in same Git blob",
}:
```
... gets stored in Git as follows:
```json
{
  "Type": "Sample.SomeNode, Sample, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
  "Node": {
    "$id": "SomeNodes/zerzrzrz.json",
    "id": "zerzrzrz",
    "someProperty": "Value stored as json"
  }
}
/*
Value stored as raw text in same Git blob
*/
```
You can also store resources as separate files:
```csharp
new Resource(node, "Some/Folder", "File.txt", new Resource.Data("Value stored in a separate file in <node path>/Resources/Some/Folder/File.txt"));
```


## Branching

```csharp
connection
    .Update(c => c.CreateOrUpdate(table with { Description = newDescription }))
    .Commit("Some message", signature, signature);
connection.Checkout("newBranch", "HEAD~1");
connection
    .Update(c => c.CreateOrUpdate(table with { Name = newName }))
    .Commit("Another message", signature, signature);
```

## History

```csharp
var commits = sut.Commits
    .QueryBy(new CommitFilter
    {
        IncludeReachableFrom = sut.Head.Tip,
        SortBy = CommitSortStrategies.Reverse | CommitSortStrategies.Topological,
    })
    .Take(5)
```

## Compare commits

```csharp
var comparison = connection.Compare("HEAD~1", repository.Head.Tip.Sha);
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
var order = connection.GetNodes<Order>(referenceCache: cache).First();
Console.WriteLine(order.Client.Id);
```

## Merge, Rebase, Cherry-pick

```csharp
// main:      A---B    A---B
//             \    ->  \   \
// newBranch:   C        C---x

connection
    .Update(c => c.CreateOrUpdate(table with { Description = newDescription }))
    .Commit("B", signature, signature);
connection.Checkout("newBranch", "HEAD~1");
connection
    .Update(c => c.CreateOrUpdate(table with { Name = newName }))
    .Commit("C", signature, signature);

sut.Merge(upstreamCommittish: "main");
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

See [Documentation][Documentation].

 [Documentation]: https://gitobjectdb.readthedocs.io

# Prerequisites

 - .NET Standard 2.0 or 2.1

# Online resources

 - [LibGit2Sharp][LibGit2Sharp] (Requires NuGet 2.7+)

 [LibGit2Sharp]: https://github.com/libgit2/libgit2sharp

# Quick contributing guide

 - Fork and clone locally
 - Create a topic specific branch. Add some nice feature. Do not forget the tests ;-)
 - Send a Pull Request to spread the fun!

# License

The MIT license (Refer to the [LICENSE][license] file).

 [license]: https://github.com/frblondin/GitObjectDb/blob/master/LICENSE
