namespace GitObjectDb.Injection;

internal delegate TService ServiceResolver<TKey, TService>(TKey key);
