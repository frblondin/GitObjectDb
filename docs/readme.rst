Home
====

GitObjectDb is designed to simplify the configuration management versioning. It does so by removing the need for hand-coding the commands needed to interact with Git.

The Git repository is used as a pure database as the files containing the serialized copy of the objects are never fetched in the filesystem. GitObjectDb only uses the blob storage provided by Git.

Here's a simple example:

1. Define your own repository data model:

.. code-block:: csharp

    [Repository]
    public class ObjectRepository
    {
        public ILazyChildren<Application> Applications { get; }
    }

.. note::

    This object contains `Applications` of type `ILazyChildren<Application>`. That's how you can create nested objects. They must be of type `ILazyChildren<Application>`._

2. Create nested object types:

.. code-block:: csharp

    [Model]
    public class Application
    {
        [Modifiable]
        public string SomeNewProperty { get; }

        public ILazyChildren<Page> Pages { get; }
    }

See `Getting Started`_ for how to manipulate data.

.. _Getting Started: basic-start.html
