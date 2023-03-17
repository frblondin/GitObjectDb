Mergins, Rebasing, Cherry-picking
=================================

Just like with Git, GitObjectDb let you do these different operations.

.. code-block:: csharp

	// main:      A---B    A---B
	//             \    ->  \   \
	// newBranch:   C        C---x

	connection
		.Update("main", c => c.CreateOrUpdate(table with { Description = newDescription }))
		.Commit(new("B", signature, signature));
	connection.Repository.Branches.Add("newBranch", "main~1");
	connection
		.Update("newBranch", c => c.CreateOrUpdate(table with { Name = newName }))
		.Commit(new("C", signature, signature));

	sut.Merge(upstreamCommittish: "main");
