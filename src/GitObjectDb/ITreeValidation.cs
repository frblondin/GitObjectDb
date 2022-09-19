using GitObjectDb.Model;
using LibGit2Sharp;

namespace GitObjectDb;

internal interface ITreeValidation
{
    void Validate(Tree tree, IDataModel model, INodeSerializer serializer);
}
