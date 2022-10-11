using System;

namespace GitObjectDb.Internal.Commands;
internal interface IGitUpdateCommand
{
    Delegate CreateOrUpdate(TreeItem item);

    Delegate Rename(TreeItem item, DataPath newPath);

    Delegate Delete(DataPath path);
}
