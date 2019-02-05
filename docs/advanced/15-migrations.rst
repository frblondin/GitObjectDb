Migrations
==========

Migrations allow to define any action that must be executed when the commit containing the migration will be processed by a pull, checkout, merge, or rebase operation. Whenever a migration has been added between the current commit and the target commir, the operation will process this migration and continue the execution once completed.

A migration is a simple type that implements the `IMigration` interface.

1. Define a new migration:

.. code-block:: csharp

    [Model]
    public partial class DummyMigration : IMigration
    {
        [DataMember]
        public bool CanDowngrade { get; }

        [DataMember]
        public bool IsIdempotent { get; }

        public void Up()
        {
            ...
        }

        public void Down()
        {
            ...
        }
    }

.. note::

    `CanDowngrade` indicates that the migration can be executed for downgrades. If not, some operations may fail if they require a downgrade.

.. note::

    `IsIdempotent` means that the migration execution will be executed at the very end of the operation.

2. Add the new migration to the repository:

.. code-block:: csharp

    var modified = repository.With(c => c
        .Add(repository,
            r => r.Migrations,
            new DummyMigration(serviceProvider, UniqueId.CreateNew(), "Dummy migration")));
    container.Commit(modified.Repository, signature, message);

3. Purge, Merge, Rebase operations will automatically invoke the migration.

Merge / Rebase processing
-------------------------

While processing a merge / rebase, the operation will process the commit containing a migration separately if and only if the migration is non-idempotent. Here is an example where `newBranch` is merged onto `master`. `E` commit will be added in order to merge `B` which contains at least one migration. In the end, `F` will contain the result of the remaining commit to be merged (`B`).

.. code-block:: csharp

    // master:    A-----D-----E---F
    //             \         /   /
    //              \   ,---'   /
    //               \ /       /
    // newBranch:     B---C---' (B contains a non-idempotent migration)
