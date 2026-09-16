using System;
using System.Collections.Generic;

namespace Magenheim.Core.Underworld;

/// <summary>
/// Bounded deterministic replay authority for remote Deepstone mutations.
/// A request identity is scoped to the authenticated peer and definition-authority generation.
/// </summary>
public sealed class UnderworldDeepstoneRequestLedger
{
    private readonly int _capacity;
    private readonly HashSet<RequestKey> _accepted = new();
    private readonly Queue<RequestKey> _order = new();

    public UnderworldDeepstoneRequestLedger(int capacity = 2048)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public int Count => _accepted.Count;

    public bool TryAdmit(long peerId, long authorityGeneration, long requestId)
    {
        if (peerId == 0L || authorityGeneration <= 0L || requestId <= 0L) return false;

        var key = new RequestKey(peerId, authorityGeneration, requestId);
        if (!_accepted.Add(key)) return false;

        _order.Enqueue(key);
        while (_order.Count > _capacity)
            _accepted.Remove(_order.Dequeue());
        return true;
    }

    public void Clear() { _accepted.Clear(); _order.Clear(); }

    private readonly record struct RequestKey(long PeerId, long AuthorityGeneration, long RequestId);
}
