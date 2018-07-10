// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly", Justification = "Not applicable")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S1944:Inappropriate casts should not be made", Justification = "Not applicable", Scope = "member", Target = "~M:GitObjectDb.Migrations.MigrationScaffolder.GetCommitMigrations(LibGit2Sharp.Commit,LibGit2Sharp.Commit)~System.Collections.Generic.IEnumerable{GitObjectDb.Migrations.IMigration}")]
