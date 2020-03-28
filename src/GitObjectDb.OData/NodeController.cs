using AutoMapper;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Results;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace GitObjectDb.OData
{
    public abstract class NodeController<TNode, TNodeDTO> : ODataController
        where TNode : Node
        where TNodeDTO : NodeDTO
    {
        private readonly IConnection _connection;
        private readonly IMapper _mapper;

        protected NodeController(IConnection connection, IMapper mapper)
        {
            _connection = connection;
            _mapper = mapper;
        }

        [EnableQuery]
        public IEnumerable<TNodeDTO> Get([FromODataUri] string? parentPath = null, [FromODataUri] string? committish = null, [FromODataUri] bool isRecursive = false)
        {
            var parent = parentPath != null ?
                _connection.Lookup<Node>(
                    new DataPath(parentPath, FileSystemStorage.DataFile),
                    committish) :
                null;
            var result = _connection.GetNodes<TNode>(parent, committish, isRecursive);
#pragma warning disable CS8974 // Converting method group to non-delegate type
            return _mapper.Map<IEnumerable<TNode>, IEnumerable<TNodeDTO>>(
                result,
                opt => opt.Items[AutoMapperProfile.ChildResolverName] = ResolveChildren);

            IEnumerable<Node> ResolveChildren(Node parent) =>
                _connection.GetNodes(parent, committish, false);
        }
    }
}
