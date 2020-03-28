using LibGit2Sharp;
using System.Linq;

namespace GitObjectDb
{
    internal class TreeValidation : ITreeValidation
    {
        public void Validate(Tree tree)
        {
            foreach (var child in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree))
            {
                switch (child.TargetType)
                {
                    case TreeEntryTargetType.Tree when child.Name == FileSystemStorage.ResourceFolder:
                    case TreeEntryTargetType.Blob when child.Name == FileSystemStorage.DataFile:
                        continue;
                    case TreeEntryTargetType.Tree:
                        ValidateTree(child);
                        break;
                    default:
                        throw new GitObjectDbException("Unexpected node type in tree hierarchy.");
                }
            }
        }

        private void ValidateTree(TreeEntry child)
        {
            foreach (var nested in child.Target.Peel<Tree>())
            {
                switch (nested.TargetType)
                {
                    case TreeEntryTargetType.Tree:
                        ValidateTreeNode(nested);
                        break;
                    default:
                        throw new GitObjectDbException("Unexpected node type in tree hierarchy.");
                }
            }
        }

        private void ValidateTreeNode(TreeEntry nested)
        {
            if (!UniqueId.TryParse(nested.Name, out _))
            {
                throw new GitObjectDbException($"Folder name '{nested.Name}' could not be parsed as a valid {nameof(UniqueId)}.");
            }
            var nestedTree = nested.Target.Peel<Tree>();
            var blob = nestedTree[FileSystemStorage.DataFile];
            if (blob == null)
            {
                throw new GitObjectDbException($"Missing data file for folder '{nested.Name}'.");
            }
            Validate(nestedTree);
        }
    }
}
