using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    [GitPath("Fields")]
    public class Field : Node
    {
        public Field(UniqueId id) : base(id)
        {
        }

        public NestedA[] A { get; set; }

        public NestedA SomeValue { get; set; }
    }

    public class NestedA
    {
        public NestedB B { get; set; }
    }

    public class NestedB
    {
        public bool IsVisible { get; set; }
    }
}
