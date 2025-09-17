Mergins, Rebasing, Cherry-picking
=================================

Just like with Git, GitObjectDb let you do these different operations.

.. code-block:: csharp

	// main:      A---B    A---B
	//             \    ->  \   \
	// newBranch:   C        C---x

	var mainUpdates = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(table with { Description = newDescription }));
	await mainUpdates.Commit(new("B", signature, signature));
	connection.Repository.Branches.Add("newBranch", "main~1");
	var newBranchUpdates = await connection.UpdateAsync("newBranch", c => c.CreateOrUpdateAsync(table with { Name = newName }));
	await newBranchUpdates.CommitAsync(new("C", signature, signature));

	var merge = await sut.MergeAsync(branchName: "newBranch", upstreamCommittish: "main");
	if (merge.Status = MergeStatus.Conflicts)
	{
		// ...
	}