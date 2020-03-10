using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Validations
{
    internal class TreeValidation : ITreeValidation
    {
        public void Validate(Tree tree)
        {
            foreach (var child in tree.Where(e => e.TargetType == TreeEntryTargetType.Tree))
            {
                switch (child.TargetType)
                {
                    case TreeEntryTargetType.Tree:
                        foreach (var nested in child.Target.Peel<Tree>())
                        {
                            switch (nested.TargetType)
                            {
                                case TreeEntryTargetType.Tree:
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
                                    break;
                                default:
                                    throw new GitObjectDbException("Unexpected node type in tree hierarchy.");
                            }
                        }
                        break;
                    case TreeEntryTargetType.Blob:
                        if (child.Name == FileSystemStorage.DataFile)
                        {
                            continue;
                        }
                        goto default;
                    default:
                        throw new GitObjectDbException("Unexpected node type in tree hierarchy.");
                }
            }
        }
    }
}
