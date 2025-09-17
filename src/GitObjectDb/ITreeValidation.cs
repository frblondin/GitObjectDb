using GitDotNet;
using GitObjectDb.Model;
using System.Threading.Tasks;

namespace GitObjectDb;

internal interface ITreeValidation
{
    Task ValidateAsync(TreeEntry tree, IDataModel model, INodeSerializer serializer);
}
