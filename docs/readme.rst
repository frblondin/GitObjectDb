Home
====

GitObjectDb is designed to simplify the configuration management versioning. It does so by removing the need for hand-coding the commands needed to interact with Git.

The Git repository is used as a pure database as the files containing the serialized copy of the objects are never fetched in the filesystem. GitObjectDb only uses the blob storage provided by Git.

Here's a simple example:

1. Add a reference to `GitObjectDb` NuGet package

2. Define your own repository data model:

.. code-block:: csharp

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

See `Getting Started`_ for how to manipulate data.

.. _Getting Started: basic-start.html
