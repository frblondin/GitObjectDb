namespace GitObjectDb.Injection;

internal delegate TService ServiceResolver<in TKey, out TService>(TKey key);
