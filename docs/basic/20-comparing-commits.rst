Comparing commits
=================

Log can be analyzed to extract changes made to nodes and resources.

.. code-block:: csharp

	var comparison = connection.Compare("main~5", "main");
	var nodeChanges = comparison.Modified.OfType<Change.NodeChange>();
