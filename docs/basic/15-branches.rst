Branches
========

Branches can be accessed to perform queries or to send commands. In the example below, you can see how to read & checkout different branches:

Direct update
-------------

.. code-block:: csharp

	connection
		.Update("main", c => c.CreateOrUpdate(table with { Description = newDescription }))
		.Commit(new("Some message", signature, signature));
	connection.Checkout("newBranch", "main~1");
	connection
		.Update("main", c => c.CreateOrUpdate(table with { Name = newName }))
		.Commit(new("Another message", signature, signature));
