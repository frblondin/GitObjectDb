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
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void TwoDifferentPropertyEdits(IConnection sut, Table table, string newDescription, string newName, Signature signature)
    {
        // Arrange
        var (b, c) = PerformActions(PerformAction.OnMain,
                                    c => c.CreateOrUpdate(table with { Description = newDescription }),
                                    c => c.CreateOrUpdate(table with { Name = newName }),
                                    sut,
                                    signature);

        // Act, Assert
        TwoDifferentPropertyEditsActAndAssert(sut, table, newDescription, newName, signature, b, c);
    }

    protected abstract void TwoDifferentPropertyEditsActAndAssert(IConnection sut, Table table, string newDescription, string newName, Signature signature, Commit b, Commit c);

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void FastForward(IConnection sut, Table table, string newDescription, Signature signature)
    {
        // Arrange
        var b = sut
            .Update("main", c => c.CreateOrUpdate(table with { Description = newDescription }))
            .Commit(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", "main~1");

        // Act, Assert
        FastForwardActAndAssert(sut, table, newDescription, signature, b);
    }

    protected abstract void FastForwardActAndAssert(IConnection sut, Table table, string newDescription, Signature signature, Commit b);

    [Test]
    [AutoDataCustomizations(typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization))]
    public void SamePropertyEdits(IConnection sut, Table table, string bValue, string cValue, Signature signature)
    {
        // Arrange
        PerformActions(PerformAction.OnMain,
                       c => c.CreateOrUpdate(table with { Description = bValue }),
                       c => c.CreateOrUpdate(table with { Description = cValue }),
                       sut,
                       signature);

        // Act, Assert
        SamePropertyEditsActAndAssert(sut, table, bValue, cValue, signature);
    }

    protected abstract void SamePropertyEditsActAndAssert(IConnection sut, Table table, string bValue, string cValue, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch)]
    public void EditOnTheirParentDeletion(PerformAction actionTarget, IConnection sut, Table parentTable, Field field, string newDescription, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.CreateOrUpdate(field with { Description = newDescription }),
                       c => c.Revert(parentTable.Path),
                       sut,
                       signature);

        // Act, Assert
        EditOnTheirParentDeletionActAndAssert(actionTarget, sut, parentTable, field, signature);
    }

    protected abstract void EditOnTheirParentDeletionActAndAssert(PerformAction actionTarget, IConnection sut, Table parentTable, Field field, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch)]
    public void DeleteChildNoConflict(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.Delete(field),
                       c => c.CreateOrUpdate(table with { Description = newDescription }),
                       sut,
                       signature);

        // Act, Assert
        DeleteChildNoConflictActAndAssert(actionTarget, sut, table, newDescription, field, signature);
    }

    protected abstract void DeleteChildNoConflictActAndAssert(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch)]
    public void AddOnTheirParentDeletion(PerformAction actionTarget, IConnection sut, Application parentApplication, Table parentTable, Signature signature)
    {
        // Arrange
        PerformActions(actionTarget,
                       c => c.CreateOrUpdate(new Field { }, parentTable),
                       c => c.Revert(parentApplication.Path),
                       sut,
                       signature);

        // Act, Assert
        AddOnTheirParentDeletionActAndAssert(actionTarget, sut, signature);
    }

    protected abstract void AddOnTheirParentDeletionActAndAssert(PerformAction actionTarget, IConnection sut, Signature signature);

    [Test]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnMain)]
    [InlineAutoDataCustomizations(new[] { typeof(DefaultServiceProviderCustomization), typeof(SoftwareCustomization) }, PerformAction.OnBranch)]
    public void RenameAndEdit(PerformAction actionTarget, IConnection sut, Field field, UniqueId newId, string newDescription, Signature signature)
    {
        // Arrange
        var newPath = new DataPath(field.Path.FolderPath,
                                   $"{newId}{Path.GetExtension(field.Path.FileName)}",
                                   field.Path.UseNodeFolders);
        var (b, c) = PerformActions(actionTarget,
                                    c => c.CreateOrUpdate(field with { Description = newDescription }),
                                    c => c.Rename(field, newPath),
                                    sut,
                                    signature);

        // Act, Assert
        RenameAndEditActAndAssert(sut, field, newDescription, signature, newPath, b, c);
    }

    protected abstract void RenameAndEditActAndAssert(IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, Commit b, Commit c);

    private static (Commit B, Commit C) PerformActions(PerformAction actionTarget,
                                                       Action<ITransformationComposer> mainAction,
                                                       Action<ITransformationComposer> secondAction,
                                                       IConnection sut,
                                                       Signature signature)
    {
        var b = sut
            .Update("main", actionTarget == PerformAction.OnMain ? mainAction : secondAction)
            .Commit(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", "main~1");
        var c = sut
            .Update("newBranch", actionTarget == PerformAction.OnBranch ? mainAction : secondAction)
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
