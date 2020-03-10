# GitObjectDb

[![NuGet Badge](https://buildstats.info/nuget/GitObjectDb)](https://www.nuget.org/packages/GitObjectDb/)
[![Build Status](https://dev.azure.com/thomas0449/GitObjectDb/_apis/build/status/frblondin.GitObjectDb?branchName=master)](https://dev.azure.com/thomas0449/GitObjectDb/_build/latest?definitionId=1&branchName=master)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=alert_status)](https://sonarcloud.io/dashboard/index/GitObjectDb)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=bugs)](https://sonarcloud.io/project/issues?id=GitObjectDb&resolved=false&types=BUG)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=coverage)](https://sonarcloud.io/component_measures?id=GitObjectDb&metric=Coverage)
[![](https://sonarcloud.io/api/project_badges/measure?project=GitObjectDb&metric=code_smells)](https://sonarcloud.io/project/issues?id=GitObjectDb&resolved=false&types=CODE_SMELL)

**GitObjectDb simplifies the configuration management versioning by backing it in Git.**

## Overview

GitObjectDb is designed to simplify the configuration management versioning. It does so by removing the need for hand-coding the commands needed to interact with Git.

The Git repository is used as a pure database as the files containing the serialized copy of the objects are never fetched in the filesystem. GitObjectDb only uses the blob storage provided by Git.

Here's a simple example:
1. Define your own repository data model:
    ```csharp
    [GitPath("Applications")]
    public class Application : Node
    {
        public Application(UniqueId id) : base(id)
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Table> GetTables(IConnection connection) => (this).GetChildren<Table>(connection);
    }
    [GitPath("Pages")]
    public class Table : Node
    {
        public Table(UniqueId id) : base(id)
        {
        }

        public string Name { get; set; }

        public string Description { get; set; }

        public IEnumerable<Field> GetFields(IConnection connection) => (this).GetChildren<Field>(connection);
    }
    ```
2. Manipulate objects as follows:
    ```csharp
	var application = connection.Get<Application>(applicationId);
	var table = new Table(UniqueId.CreateNew()) { ... };
	connection
	    .Update(c => c.Add(table, application))
		.Commit("Added table.", author, committer);
    ```

### Documentation

See [Documentation][Documentation].

 [Documentation]: https://gitobjectdb.readthedocs.io

## Prerequisites

 - .NET Standard 2.0

## Online resources

 - [LibGit2Sharp][LibGit2Sharp] (Requires NuGet 2.7+)

 [LibGit2Sharp]: https://github.com/libgit2/libgit2sharp

## Quick contributing guide

 - Fork and clone locally
 - Create a topic specific branch. Add some nice feature. Do not forget the tests ;-)
 - Send a Pull Request to spread the fun!

## License

The MIT license (Refer to the [LICENSE][license] file)

 [license]: https://github.com/frblondin/GitObjectDb/blob/master/LICENSE
