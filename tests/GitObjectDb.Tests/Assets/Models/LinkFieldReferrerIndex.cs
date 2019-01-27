using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GitObjectDb.Models;
using GitObjectDb.Tests.Assets.Models;

namespace GitObjectDb.Tests.Assets.Models
{
    [Index]
#pragma warning disable CA1710 // Identifiers should have correct suffix
    public partial class LinkFieldReferrerIndex
#pragma warning restore CA1710 // Identifiers should have correct suffix
    {
        partial void ComputeKeys(IModelObject node, ISet<string> result)
        {
            // The ComputeKeys method gets invoked by GitObjectDb for any modified node
            // You can index whatever you want. Just return the key(s) for an object
            if (node is Field field)
            {
                var link = field.Content.MatchOrDefault(() => null, l => l.Target.Path);
                if (link != null)
                {
                    result.Add(link.FullPath);
                }
            }
        }
    }
}
