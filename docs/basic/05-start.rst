Start
=====

Once the data model is created (see home page), you can initialize a new repository like this:

.. code-block:: csharp

    var serviceProvider = new ServiceCollection()
        .AddGitObjectDb()
        .BuildServiceProvider();
    var factory = serviceProvider.GetRequiredService<ConnectionFactory>();
    var connection = factory(path);

.. note::
    Once a connection has been established, the repository can be queried / updated using the connection object.