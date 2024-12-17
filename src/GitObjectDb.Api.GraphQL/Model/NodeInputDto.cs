using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitObjectDb.Api.GraphQL.Model;
public abstract class NodeInputDto
{
}

#pragma warning disable SA1402 // File may only contain a single type
public abstract class NodeInputDto<TNode> : NodeInputDto
#pragma warning restore SA1402 // File may only contain a single type
{
}