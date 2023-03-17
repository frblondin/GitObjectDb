Start
=====

Once the data model is created (see home page), you can initialize a new repository like this:

.. code-block:: csharp

    var serviceProvider = new ServiceCollection()
        .AddGitObjectDb()
        .AddGitObjectDbSystemTextJson()
        .AddSingleton(new ConventionBaseModelBuilder()
            .RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
            .Build())
        .BuildServiceProvider();
    var factory = serviceProvider.GetRequiredService<ConnectionFactory>();
    var connection = factory(path);

In the example above, a Json serializer has been used. Alternative serializers exist, like Yaml (see GitObjectDb nuget packages.)

.. note::
    Once a connection has been established, the repository can be queried / updated using the connection object.
