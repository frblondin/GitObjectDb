using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using GitObjectDb.Tests.Assets.Tools;
using LibGit2Sharp;
using Models.Software;
using NUnit.Framework;
using System;
using System.IO;

namespace GitObjectDb.Tests;

public abstract class BranchMergerFixture : DisposeArguments
{
    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.FastImport)]
    public void TwoDifferentPropertyEdits(CommitCommandType commitType, IConnection sut, Table table, string newDescription, string newName, Signature signature)
    {
        // Arrange
        var (b, c) = PerformActions(PerformAction.OnMain,
                                    c => c.CreateOrUpdate(table with { Description = newDescription }),
                                    c => c.CreateOrUpdate(table with { Name = newName }),
                                    sut,
                                    signature,
                                    commitType);

        // Act, Assert
        TwoDifferentPropertyEditsActAndAssert(commitType, sut, table, newDescription, newName, signature, b, c);
    }

    protected abstract void TwoDifferentPropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, string newName, Signature signature, Commit b, Commit c);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.FastImport)]
    public void FastForward(CommitCommandType commitType, IConnection sut, Table table, string newDescription, Signature signature)
    {
        // Arrange
        var b = sut
            .Update("main", c => c.CreateOrUpdate(table with { Description = newDescription }), commitType)
            .Commit(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", "main~1");

        // Act, Assert
        FastForwardActAndAssert(commitType, sut, table, newDescription, signature, b);
    }

    protected abstract void FastForwardActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string newDescription, Signature signature, Commit b);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, CommitCommandType.FastImport)]
    public void SamePropertyEdits(CommitCommandType commitType, IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Arrange
        PerformActions(PerformAction.OnMain,
                       c => c.CreateOrUpdate(table with { Description = bValue }),
                       c => c.CreateOrUpdate(table with { Description = cValue }),
                       sut,
                       signature,
                       commitType);

        // Act, Assert
        SamePropertyEditsActAndAssert(commitType, sut, table, bValue, cValue, signature);
    }

    protected abstract void SamePropertyEditsActAndAssert(CommitCommandType commitType, IConnection sut, Table table, string bValue, string cValue, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.FastImport)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.FastImport)]
    public void EditOnTheirParentDeletion(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table parentTable, Field field, string newDescription, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.CreateOrUpdate(field with { Description = newDescription }),
                       c => c.Delete(parentTable.Path),
                       sut,
                       signature,
                       commitType);

        // Act, Assert
        EditOnTheirParentDeletionActAndAssert(actionTarget, commitType, sut, parentTable, field, signature);
    }

    protected abstract void EditOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table parentTable, Field field, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.FastImport)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.FastImport)]
    public void DeleteChildNoConflict(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.Delete(field),
                       c => c.CreateOrUpdate(table with { Description = newDescription }),
                       sut,
                       signature,
                       commitType);

        // Act, Assert
        DeleteChildNoConflictActAndAssert(actionTarget, commitType, sut, table, newDescription, field, signature);
    }

    protected abstract void DeleteChildNoConflictActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Table table, string newDescription, Field field, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.FastImport)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.FastImport)]
    public void AddOnTheirParentDeletion(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Application parentApplication, Table parentTable, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.CreateOrUpdate(new Field { }, parentTable),
                       c => c.Delete(parentApplication.Path),
                       sut,
                       signature,
                       commitType);

        // Act, Assert
        AddOnTheirParentDeletionActAndAssert(actionTarget, commitType, sut, signature);
    }

    protected abstract void AddOnTheirParentDeletionActAndAssert(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain, CommitCommandType.FastImport)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.Normal)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch, CommitCommandType.FastImport)]
    public void RenameAndEdit(PerformAction actionTarget, CommitCommandType commitType, IConnection sut, Field field, UniqueId newId, string newDescription, Signature signature)
    {
        // Arrange
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"{newId}{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        var (b, c) = PerformActions(actionTarget,
                                    c => c.CreateOrUpdate(field with { Description = newDescription }),
                                    c => c.Rename(field, newPath),
                                    sut,
                                    signature,
                                    commitType);

        // Act, Assert
        RenameAndEditActAndAssert(commitType, sut, field, newDescription, signature, newPath, b, c);
    }

    protected abstract void RenameAndEditActAndAssert(CommitCommandType commitType, IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, Commit b, Commit c);

    private static (Commit B, Commit C) PerformActions(PerformAction actionTarget,
                                                       Action<ITransformationComposer> mainAction,
                                                       Action<ITransformationComposer> secondAction,
                                                       IConnection sut,
                                                       Signature signature,
                                                       CommitCommandType commitType = CommitCommandType.Auto)
    {
        var b = sut
            .Update("main", actionTarget == PerformAction.OnMain ? mainAction : secondAction, commitType)
            .Commit(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", "main~1");
        var c = sut
            .Update("newBranch", actionTarget == PerformAction.OnBranch ? mainAction : secondAction, commitType)
            .Commit(new("C", signature, signature));
        return (b, c);
    }

#pragma warning disable SA1201 // Elements should appear in the correct order
#pragma warning disable SA1602 // Enumeration items should be documented
    public enum PerformAction
    {
        OnMain,
        OnBranch,
    }
}
