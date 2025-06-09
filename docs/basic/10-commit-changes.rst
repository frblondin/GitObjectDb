Commit changes
==============

Changes to the repository can be done using a composable syntax. There are two ways to commit changes to the repositories:

Direct update
-------------

.. code-block:: csharp

	var updates = await connection.UpdateAsync("main", c => c.CreateOrUpdateAsync(table, parent: application));
	await updates.CommitAsync("Added table.", author, committer);

.. note::
    Within the UpdateAsync(...) method, multiple transformations can be defined using nested calls.


Stage in the index, then commit
-------------------------------

Previous method stores transformation in-memory then creates a commit. In the case where you need to store transformations made persistent to be committed later (like what you would do when using Git with your files), you can use the index method:

.. code-block:: csharp

	var index = await connection.GetIndexAsync("main", c => c.CreateOrUpdateAsync(table, parent: application));
    await index.CommitAsync("Added table.", author, committer);

.. note::
    Since GitObjectDb uses a bare repository, the internal Git index database file cannot be used. GitObjectDb uses its own independant dbindex file that has the advantage that multiple indices can be used simultaneously on different branches.