using System;

namespace GitObjectDb.Internal.Commands;
internal interface IGitUpdateCommand
{
    ApplyUpdate CreateOrUpdate(TreeItem item);

    ApplyUpdate Rename(TreeItem item, DataPath newPath);

    ApplyUpdate Delete(DataPath path);
}