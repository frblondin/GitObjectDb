Node versioning
===============

There can be scenario where you want to introduce changes to the code-first metadata that will change the way the data gets serialized. GitObjectDb let you define how the old nodes that have been stored in past commits can be adapted at runtime to the latest version:

.. code-block:: csharp

	[GitFolder(FolderName = "Items", UseNodeFolders = false)]
	[IsDeprecatedNodeType(typeof(SomeNodeV2))]
	private record SomeNodeV1 : Node
	{
		public int Flags { get; set; }
	}

	[GitFolder(FolderName = "Items", UseNodeFolders = false)]
	private record SomeNodeV2 : Node
	{
		public BindingFlags TypedFlags { get; set; }
	}

You then want to introduce a new change so that the `Flags` property contains more meaningful information, relying on enums:

.. code-block:: csharp

	[GitFolder(FolderName = "Items", UseNodeFolders = false)]
	private record SomeNodeV2 : Node
	{
		public BindingFlags TypedFlags { get; set; }
	}

All you need to do is to #1 add the `[IsDeprecatedNodeType(typeof(SomeNodeV2))]` attribute. This will instruct the deserializer to convert nodes to new version, using a converter. #2 converter needs to be provided in the model. You can use AutoMapper or other tools at your convenience.

.. code-block:: csharp

	[GitFolder(FolderName = "Items", UseNodeFolders = false)]
	[IsDeprecatedNodeType(typeof(SomeNodeV2))]
	private record SomeNodeV1 : Node
	{
		// ...
	}
	var model = new ConventionBaseModelBuilder()
		.RegisterType<SomeNodeV1>()
		.RegisterType<SomeNodeV2>()
		.AddDeprecatedNodeUpdater(UpdateDeprecatedNode)
		.Build();
	Node UpdateDeprecatedNode(Node old, Type targetType)
	{
		var nodeV1 = (SomeNodeV1)old;
		return new SomeNodeV2
		{
			Id = old.Id,
			TypedFlags = (BindingFlags)nodeV1.Flags,
		};
	}
