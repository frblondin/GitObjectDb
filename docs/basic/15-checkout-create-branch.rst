Checkout / Create Branch
========================

A branch can be checked out using the following command:

.. code-block:: csharp

    var newBranch = container.Checkout(
        repository.Id,
        "newBranch",
        createNewBranch: true);
