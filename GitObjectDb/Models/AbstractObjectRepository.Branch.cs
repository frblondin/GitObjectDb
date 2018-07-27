using GitObjectDb.Compare;
using GitObjectDb.Git;
using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GitObjectDb.Models
{
    public partial class AbstractObjectRepository
    {
        /// <inheritdoc />
        public Branch Checkout(string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var branch = repository.Branches[branchName];
                repository.Refs.MoveHeadTarget(branch.CanonicalName);
                return branch;
            });
        }

        /// <inheritdoc />
        public Branch Branch(string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return _repositoryProvider.Execute(_repositoryDescription, repository =>
            {
                var branch = repository.CreateBranch(branchName);
                repository.Refs.MoveHeadTarget(branch.CanonicalName);
                return branch;
            });
        }

        /// <inheritdoc />
        public IMetadataTreeMerge Merge(string branchName)
        {
            if (branchName == null)
            {
                throw new ArgumentNullException(nameof(branchName));
            }

            return _metadataTreeMergeFactory(_repositoryDescription, this, branchName);
        }
    }
}
