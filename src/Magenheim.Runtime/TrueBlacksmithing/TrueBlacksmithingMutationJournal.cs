using System;
using System.Collections.Generic;
using BepInEx.Logging;

namespace Magenheim.Runtime.TrueBlacksmithing;

/// <summary>
/// Owns reversible True Blacksmithing mutations. A mutation must register its
/// rollback before it is applied so activation can never intentionally strand
/// Magenheim in a partially converted crafting state.
/// </summary>
internal sealed class TrueBlacksmithingMutationJournal : IDisposable
{
    private sealed class RollbackEntry
    {
        internal RollbackEntry(string description, Action rollback)
        {
            Description = description;
            Rollback = rollback;
        }

        internal string Description { get; }
        internal Action Rollback { get; }
    }

    private readonly ManualLogSource _logger;
    private readonly List<RollbackEntry> _entries = new List<RollbackEntry>();
    private bool _rolledBack;
    private bool _rollbackStarted;

    internal bool HasPendingRollback => _entries.Count != 0;

    internal TrueBlacksmithingMutationJournal(ManualLogSource logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    internal void Apply(string description, Action apply, Action rollback)
    {
        if (_rollbackStarted)
        {
            throw new InvalidOperationException("Cannot apply a True Blacksmithing mutation after rollback.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Mutation description is required.", nameof(description));
        }

        if (apply is null) throw new ArgumentNullException(nameof(apply));
        if (rollback is null) throw new ArgumentNullException(nameof(rollback));

        _entries.Add(new RollbackEntry(description, rollback));

        try
        {
            apply();
        }
        catch
        {
            RollbackAll();
            throw;
        }
    }

    internal void RollbackAll()
    {
        if (_rolledBack) return;
        _rollbackStarted = true;

        for (var index = _entries.Count - 1; index >= 0; index--)
        {
            var entry = _entries[index];

            try
            {
                entry.Rollback();
                _entries.RemoveAt(index);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    $"True Blacksmithing rollback failed for '{entry.Description}': {exception}");
            }
        }

        // Failed restoration remains available for the next safe teardown attempt. Successful
        // reversals are removed so retries cannot mutate restored recipes a second time.
        _rolledBack = _entries.Count == 0;
    }

    public void Dispose()
    {
        RollbackAll();
    }
}
