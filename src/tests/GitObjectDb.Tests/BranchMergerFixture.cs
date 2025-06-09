using AutoFixture;
using GitDotNet;
using GitObjectDb.Tests.Assets;
using GitObjectDb.Tests.Assets.Data.Software;
using Models.Software;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GitObjectDb.Tests;

public abstract class BranchMergerFixture
{
    [Test]
    public async Task TwoDifferentPropertyEdits()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();
        var newDescription = fixture.Create<string>();
        var newName = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var (b, c) = await PerformActionsAsync(PerformAction.OnMain,
            c => c.CreateOrUpdateAsync(table with { Description = newDescription }),
            c => c.CreateOrUpdateAsync(table with { Name = newName }),
            sut,
            signature);

        // Act, Assert
        await TwoDifferentPropertyEditsActAndAssertAsync(sut, table, newDescription, newName, signature, b, c);
    }

    protected abstract Task TwoDifferentPropertyEditsActAndAssertAsync(IConnection sut, Table table, string newDescription, string newName, Signature signature, CommitEntry b, CommitEntry c);

    [Test]
    public async Task FastForward()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();
        var newDescription = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var bChanges = await sut.UpdateAsync("main", c => c.CreateOrUpdateAsync(table with { Description = newDescription }));
        var b = await bChanges.CommitAsync(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", await sut.Repository.GetCommittishAsync("main~1"));

        // Act, Assert
        await FastForwardActAndAssertAsync(sut, table, newDescription, signature, b);
    }

    protected abstract Task FastForwardActAndAssertAsync(IConnection sut, Table table, string newDescription, Signature signature, CommitEntry b);

    [Test]
    public async Task SamePropertyEdits()
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();
        var bValue = fixture.Create<string>();
        var cValue = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        await PerformActionsAsync(PerformAction.OnMain,
            c => c.CreateOrUpdateAsync(table with { Description = bValue }),
            c => c.CreateOrUpdateAsync(table with { Description = cValue }),
            sut,
            signature);

        // Act, Assert
        await SamePropertyEditsActAndAssertAsync(sut, table, bValue, cValue, signature);
    }

    protected abstract Task SamePropertyEditsActAndAssertAsync(IConnection sut, Table table, string bValue, string cValue, Signature signature);

    [Test]
    [TestCase(PerformAction.OnMain)]
    [TestCase(PerformAction.OnBranch)]
    public async Task EditOnTheirParentDeletion(PerformAction actionTarget)
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var parentTable = fixture.Create<Table>();
        var field = fixture.Create<Field>();
        var newDescription = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        await PerformActionsAsync(actionTarget,
            c => c.CreateOrUpdateAsync(field with { Description = newDescription }),
            c => c.RevertAsync(parentTable.Path),
            sut,
            signature);

        // Act, Assert
        await EditOnTheirParentDeletionActAndAssertAsync(actionTarget, sut, parentTable, field, signature);
    }

    protected abstract Task EditOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table parentTable, Field field, Signature signature);

    [Test]
    [TestCase(PerformAction.OnMain)]
    [TestCase(PerformAction.OnBranch)]
    public async Task DeleteChildNoConflict(PerformAction actionTarget)
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var table = fixture.Create<Table>();
        var newDescription = fixture.Create<string>();
        var field = fixture.Create<Field>();
        var signature = fixture.Create<Signature>();

        await PerformActionsAsync(actionTarget,
            c => c.DeleteAsync(field),
            c => c.CreateOrUpdateAsync(table with { Description = newDescription }),
            sut,
            signature);

        // Act, Assert
        await DeleteChildNoConflictActAndAssertAsync(actionTarget, sut, table, newDescription, field, signature);
    }

    protected abstract Task DeleteChildNoConflictActAndAssertAsync(PerformAction actionTarget, IConnection sut, Table table, string newDescription, Field field, Signature signature);

    [Test]
    [TestCase(PerformAction.OnMain)]
    [TestCase(PerformAction.OnBranch)]
    public async Task AddOnTheirParentDeletion(PerformAction actionTarget)
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var parentApplication = fixture.Create<Application>();
        var parentTable = fixture.Create<Table>();
        var signature = fixture.Create<Signature>();

        await PerformActionsAsync(actionTarget,
            c => c.CreateOrUpdateAsync(new Field { }, parentTable),
            c => c.RevertAsync(parentApplication.Path),
            sut,
            signature);

        // Act, Assert
        await AddOnTheirParentDeletionActAndAssertAsync(actionTarget, sut, signature);
    }

    protected abstract Task AddOnTheirParentDeletionActAndAssertAsync(PerformAction actionTarget, IConnection sut, Signature signature);

    [Test]
    [TestCase(PerformAction.OnMain)]
    [TestCase(PerformAction.OnBranch)]
    public async Task RenameAndEdit(PerformAction actionTarget)
    {
        // Arrange
        var fixture = await new Fixture().Customize(new DefaultServiceProviderCustomization()).CustomizeAsync<SoftwareCustomization>();
        var sut = fixture.Create<IConnection>();
        var field = fixture.Create<Field>();
        var newId = fixture.Create<UniqueId>();
        var newDescription = fixture.Create<string>();
        var signature = fixture.Create<Signature>();

        var newPath = new DataPath(field.Path.FolderPath,
            $"{newId}{Path.GetExtension(field.Path.FileName)}",
            field.Path.UseNodeFolders);
        var (b, c) = await PerformActionsAsync(actionTarget,
            c => c.CreateOrUpdateAsync(field with { Description = newDescription }),
            c => c.RenameAsync(field, newPath),
            sut,
            signature);

        // Act, Assert
        await RenameAndEditActAndAssertAsync(sut, field, newDescription, signature, newPath, b, c);
    }

    protected abstract Task RenameAndEditActAndAssertAsync(IConnection sut, Field field, string newDescription, Signature signature, DataPath newPath, CommitEntry b, CommitEntry c);

    private static async Task<(CommitEntry B, CommitEntry C)> PerformActionsAsync(PerformAction actionTarget,
        Func<IChangeComposer, Task> mainAction,
        Func<IChangeComposer, Task> secondAction,
        IConnection sut,
        Signature signature)
    {
        var bChanges = await sut.UpdateAsync("main", actionTarget == PerformAction.OnMain ? mainAction : secondAction);
        var b = await bChanges.CommitAsync(new("B", signature, signature));
        sut.Repository.Branches.Add("newBranch", await sut.Repository.GetCommittishAsync("main~1"));
        var cChanges = await sut.UpdateAsync("newBranch", actionTarget == PerformAction.OnBranch ? mainAction : secondAction);
        var c = await cChanges.CommitAsync(new("C", signature, signature));
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
