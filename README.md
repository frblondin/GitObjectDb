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
1. Define your own data model by inheriting from `AbstractObjectRepository`:
    ```cs
    public class ObjectRepository : AbstractObjectRepository
    {
        public ObjectRepository(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<IMigration> migrations, ILazyChildren<Application> applications)
            : base(serviceProvider, id, name, migrations)
        {
            Applications = (applications ?? throw new ArgumentNullException(nameof(applications))).AttachToParent(this);
        }
    
        public ILazyChildren<Application> Applications { get; }
    }
    ```
    _Note that this object contains `Applications` of type `ILazyChildren<Application>`. That's how you can create nested objects. They must be of type `ILazyChildren<Application>`._
2. Create nested object types:
    ```cs
    [DataContract]
    public class Application : AbstractModel
    {
        public Application(IServiceProvider serviceProvider, Guid id, string name, ILazyChildren<Page> pages)
            : base(serviceProvider, id, name)
        {
            Pages = (pages ?? throw new ArgumentNullException(nameof(pages))).AttachToParent(this);
        }

        public ILazyChildren<Page> Pages { get; }
    }
    ```
3. Basic commands
   - Initialize a new repository
        ```cs
        var repo = new ObjectRepository(...);
        repo.SaveInNewRepository(signature, message, new RepositoryDescription(path));
        ```
   - Commit a new change
        ```cs
        var modifiedPage = page.With(p => p.Name == "modified");
        instance.Commit(modifiedPage.Repository, signature, message);
        ```
    - [Branch management](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Models/ObjectRepositoryTests.Branch.cs)
    - Migrations:
        
        Migrations allows to define any action that must be executed when the commit containing the migration will be processed by a pull. See the [unit tests](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Migrations/MigrationTests.cs) for more details.
    - [Pre/post commit hook](https://github.com/frblondin/GitObjectDb/blob/master/GitObjectDb.Tests/Git/Hooks/GitHooksTests.cs)

## Prerequisites

 - **Windows:** .NET 4.0+
 - **Linux/Mac OS X:** Mono 3.6+

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
