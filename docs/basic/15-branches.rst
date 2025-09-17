Branches
========

Branches can be accessed to perform queries or to send commands. In the example below, you can see how to read & checkout different branches:

Direct update
-------------

.. code-block:: csharp

    connection.Repository.Branches.Add("newBranch", "main~1");
	var updates = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(table with { Name = newName }));
	await updates.CommitAsync(new("Another message", signature, signature));
