Commit changes
==============

Changes to the repository can be done using two syntaxes.

The simple syntax only changes one existing object propery value:

.. code-block:: csharp

    var modified = repo.With(page, p => p.Name, "modified");
    container.Commit(modified.Repository, signature, message);

This more advanced syntax can also be used to add/remove children or to compose changes:

.. code-block:: csharp

    var modified = repository.With(c => c
        .Update(field, f => f.Name, "modified field name")
        .Update(field, f => f.Content, FieldContent.NewLink(
            new FieldLinkContent(
                new LazyLink<Page>(
                    container,
                    newLinkedPage))))
        .Update(page, p => p.Name, "modified page name"));
    container.Commit(modified.Repository, signature, message);
