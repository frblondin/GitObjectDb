Index Management
================

An index is a data structure that improves the speed of data retrieval operations on the model objects at the cost of additional writes and storage space to maintain the index data structure. Indexes are used to quickly locate data without having to search every instance in the repository model. Indexes can be created using one or more properties, providing the basis for rapid random lookups.

1. Define a new index:

.. code-block:: csharp

    [Index]
    public partial class PersonAgeIndex
    {
        partial void ComputeKeys(IModelObject node, ISet<string> result)
        {
            // The ComputeKeys method gets invoked by GitObjectDb for any modified node
            // You can index whatever you want. Just return the key(s) for an object
            if (node is Person person)
            {
                result.Add(person.Age.ToString());
            }
        }
    }

.. note::

    An index key has to be a string.

2. Add the new index to the repository to make it active:

.. code-block:: csharp

    var modified = repository.With(c => c
        .Add(repository,
            r => r.Indexes,
            new PersonAgeIndex(serviceProvider, UniqueId.CreateNew(), "Index by age")));
    container.Commit(modified.Repository, signature, message);

Index can be used this way:

.. code-block:: csharp

    var index = repository.Indexes.Single(i => i is PersonAgeIndex);
    var referrers = index["42"];