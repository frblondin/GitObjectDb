# GitObjectDb

[![](https://ci.appveyor.com/api/projects/status/github/frblondin/gitobjectdb)](https://ci.appveyor.com/project/frblondin/gitobjectdb)
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
    [Repository]
    public class ObjectRepository
    {
        public ILazyChildren<Application> Applications { get; }
    }
    ```
    _Note that this object contains `Applications` of type `ILazyChildren<Application>`. That's how you can create nested objects. They must be of type `ILazyChildren<Application>`._
2. Create nested object types:
    ```csharp
    [Model]
    public class Application
    {
        [Modifiable]
        public string SomeNewProperty { get; }

        public ILazyChildren<Page> Pages { get; }
    }
    ```
3. Basic commands
   - Initialize a new repository
        ```csharp
        var container = new ObjectRepositoryContainer<ObjectRepository>(serviceProvider, path);
        var repo = new ObjectRepository(...);
        container.AddRepository(repo, signature, message);
        ```
   - Commit a new change
        ```csharp
        var modified = repo.With(page, p => p.Name, "modified");
        container.Commit(modified.Repository, signature, message);
        ```
   - Commit multiple changes
        ```csharp
        var modified = repository.With(c => c
            .Update(field, f => f.Name, "modified field name")
            .Update(field, f => f.Content, FieldContent.NewLink(new FieldLinkContent(new LazyLink<Page>(container, newLinkedPage))))
            .Update(page, p => p.Name, "modified page name"));
        container.Commit(modified.Repository, signature, message);
        ```
    - Branch management: see [branch & merges](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Models/ObjectRepositoryTests.Branch.cs) and [rebase](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Models/ObjectRepositoryTests.Rebase.cs) unit tests.
    - Migrations: migrations allows to define any action that must be executed when the commit containing the migration will be processed by a pull. See the [unit tests](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Migrations/MigrationTests.cs) for more details.
    - [Pre/post commit & merge hook](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Git/Hooks/GitHooksTests.cs)
	- Simple validation: see [unit tests](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Validations/ModelValidationTests.cs) for more information.

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
