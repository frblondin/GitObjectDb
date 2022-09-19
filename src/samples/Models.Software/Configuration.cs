using GitObjectDb;
using GitObjectDb.Model;

namespace Models.Software
{
    [GitFolder(FolderName = "Configuration")]
    public record Configuration : Node
    {
    }
}
