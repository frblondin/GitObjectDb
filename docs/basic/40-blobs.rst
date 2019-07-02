Blobs
=====

Usually, properties are serialized in the repository as Json properties. You can decide to store larger objects in separate files. The file will be stored next to the data file (`data.json`). Its name is `<propertyName>.json`.

.. code-block:: csharp

    [Model]
    public partial class Car
    {
        [DataMember]
        [Modifiable]
        public StringBlob Manual { get; }
    }

*Note*

While merging/rebasing changes, the blob content will be used as a whole. In the case where conflicts are detected, a line by line comparison can still be performed using a 3-way merge library such as `DiffLib <https://github.com/lassevk/DiffLib>`_.