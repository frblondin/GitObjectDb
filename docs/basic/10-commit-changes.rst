Commit changes
==============

Changes to the repository can be done using a composable syntax:

.. code-block:: csharp

	connection
	    .Update(c => c.CreateOrUpdate(table, parent: application))
		.Commit("Added table.", author, committer);


.. note::
    Within the Update(...) method, multiple transformations can be defined using nested calls.