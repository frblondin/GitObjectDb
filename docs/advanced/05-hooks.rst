Hooks
=====

Hooks allow listeners to subscribe to Git events. Four event types are supported:

- `CommitStarted` gets fired whenever a commit is about to be made. The `CommitStartedEventArgs` contains all changes and the commit message.

- `CommitCompleted` gets fired once the commit has completed. The `CommitCompletedEventArgs` contains all changes, the commit message, and the commit ID.

- `MergeStarted` gets fired before the merge process gets started. The `MergeStartedEventArgs` contains all changes.

- `MergeCompleted` gets fired once the merge has completed. The `CommitStartedEventArgs` contains all changes and the commit message.

.. note::

    An exception can be thrown in `XXXStarted` events to prevent the operation from being executed.
