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
                    case TreeEntryTargetType.Tree when child.Name != FileSystemStorage.ResourceFolder:
                        ValidateTree(child);
                        break;
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
            var blob = nestedTree[$"{nested.Name}.json"];
            if (blob == null)
            {
                throw new GitObjectDbException($"Missing data file for node '{nested.Name}'.");
            }
            Validate(nestedTree);
        }
    }
}
