using System;
using System.Collections.Generic;
using System.Text;

namespace GitObjectDb.Tests.Assets.Models.Software
{
    [GitPath("Configuration")]
    public class Configuration : Node
    {
        public Configuration(UniqueId id) : base(id)
        {
        }
    }
}
