using LibGit2Sharp;

namespace GitObjectDb.Validations
{
    internal interface ITreeValidation
    {
        void Validate(Tree tree);
    }
}