Start
=====

Once the data model is created (see home page), you can initialize a new repository like this:

.. code-block:: csharp

    var serviceProvider = new ServiceCollection()
        .AddGitObjectDb()
        .BuildServiceProvider();
    var container = new ObjectRepositoryContainer<ObjectRepository>(
        serviceProvider,
        path);
   
    // Optional: add new repository
    var repo = new ObjectRepository(...);
    container.AddRepository(repo, signature, message);

.. note::
    Once a repository has been added, the container will load the repositories automatically when it gets instantiated.