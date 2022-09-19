using LibGit2Sharp;

namespace GitObjectDb.Api.Model;

public class DeltaDto<TNodeDto>
{
    private readonly Commit _commit;

    public DeltaDto(TNodeDto old, TNodeDto @new, Commit commit, bool deleted)
    {
        Old = old;
        New = @new;
        _commit = commit;
        Deleted = deleted;
    }

    public TNodeDto Old { get; set; }

    public TNodeDto New { get; set; }

    public string UpdatedAt => _commit.Id.Sha;

    public bool Deleted { get; set; }
}
