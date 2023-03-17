Node references
===============

Node references allows linking existing nodes in a repository.

.. code-block:: csharp

	public record Order : Node
	{
		public Client Client { get; set; }
		// ...
	}
	public record Client : Node
	{
		// ...
	}
	// Nodes get loaded with their references (using a shared )
	var cache = new Dictionary<DataPath, ITreeItem>();
	var order = connection.GetNodes<Order>("main", referenceCache: cache).First();
	Console.WriteLine(order.Client.Id);
