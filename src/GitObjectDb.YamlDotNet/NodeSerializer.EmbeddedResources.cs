using System.IO;
using System.Text;
using YamlDotNet.Core;
using CommentEvent = YamlDotNet.Core.Events.Comment;
using CommentToken = YamlDotNet.Core.Tokens.Comment;

namespace GitObjectDb.YamlDotNet;

internal partial class NodeSerializer : INodeSerializer
{
    private static string? ReadEmbeddedResource(Stream stream)
    {
        stream.Position = 0L;
        using var reader = new StreamReader(stream);
        var scanner = new Scanner(reader, skipComments: false);
        var comment = default(StringBuilder?);
        while (scanner.MoveNext())
        {
            if (scanner.Current is CommentToken c &&
                c.Start.Column == 1)
            {
                comment?.Append('\n');
                comment ??= new StringBuilder();
                comment.Append(c.Value);
            }
        }
        return comment?.ToString();
    }

    private static void WriteEmbeddedResource(IEmitter emitter, Node node)
    {
        if (node.EmbeddedResource is not null)
        {
            emitter.Emit(new CommentEvent(node.EmbeddedResource, false));
        }
    }
}
