Cherry Pick
===========

Simple example
--------------

.. code-block:: csharp

    var cherryPick = container.CherryPick(sut.Id, commitId);
    switch (cherryPick.Status)
    {
        case CherryPickStatus.CherryPicked: // All done!
            ...
        case CherryPickStatus.Conflicts: // Conflicts :-(
            ...
    }

Resolve conflicts
-----------------

.. code-block:: csharp

    // master:    A---B
    //             \
    // newBranch:   C   ->   A---C---B

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
    var cherryPick = container.CherryPick(sut.Id, b.CommitId);
    if (cherryPick.Status == CherryPickStatus.Conflicts)
    {
       // Merge conflicts by getting conflicting chunk changes
       // and calling c.Resolve(...)
    }

    // An exception is thrown if there are remaining unresolved conflicts
    cherryPick.Commit(signature);
