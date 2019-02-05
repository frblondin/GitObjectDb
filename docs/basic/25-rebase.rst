Rebase
======

Simple example
--------------

.. code-block:: csharp

    var rebase = container.Rebase(sut.Id, "master");
    switch (rebase.Status)
    {
        case RebaseStatus.Complete: // All done!
            ...
        case RebaseStatus.Conflicts: // Conflicts :-(
            ...
        case RebaseStatus.Stop: // User requested stop point
            ...
    }

Resolve conflicts
-----------------

.. code-block:: csharp

    // master:    A---B
    //             \
    // newBranch:   C   ->   A---B---C

    var updateName = a.With(
        a.Applications[0].Pages[0],
        p => p.Name,
        "modified name");
    var b = container.Commit(updateName.Repository, signature, message); // B
    a = container.Checkout(a.Id, "newBranch", createNewBranch: true, "HEAD~1");
    var updateDescription = a.With(
        a.Applications[0].Pages[0],
        p => p.Description,
        "modified description");
    container.Commit(updateDescription.Repository, signature, message); // C
    var rebase = container.Rebase(sut.Id, "master");
