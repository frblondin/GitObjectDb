Merge
=====

Simple example
--------------

.. code-block:: csharp

    var commit = container.Merge(repo.Id, "newBranch").Apply();

Resolve conflicts
-----------------

.. code-block:: csharp

    // master:    A---C---D
    //             \     /
    // newBranch:   B---'

    repo = container.Checkout(repo.Id, "newBranch", true);
    var updateName = repo.With(
        repo.Applications[0].Pages[0],
        p => p.Name,
        "modified name");
    container.Commit(updateName.Repository, signature, message); // B
    var a = container.Checkout(repo.Id, "master"); // A
    var updateDescription = a.With(
        a.Applications[0].Pages[0],
        p => p.Description,
        "modified description");
    container.Commit(updateDescription.Repository, signature, message); // C
    var merge = container.Merge(repo.Id, "newBranch");
    if (merge.ModifiedChunks.Any(c => c.IsInConflict))
    {
       // Merge conflicts by getting conflicting chunk changes
       // and calling c.Resolve(...)
    }

    // An exception is thrown if there are remaining unresolved conflicts
    var d = merge.Apply(signature); // D
