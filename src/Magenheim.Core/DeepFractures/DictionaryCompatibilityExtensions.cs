using System.Collections.Generic;

namespace Magenheim.Core.DeepFractures;

/// <summary>
/// Compatibility shim for target frameworks where Dictionary.TryAdd is not available.
/// Kept local to Deep Fractures so the validator retains fail-closed duplicate handling
/// without raising the Magenheim.Core target above netstandard2.0.
/// </summary>
internal static class DictionaryCompatibilityExtensions
{
    internal static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key))
            return false;

        dictionary.Add(key, value);
        return true;
    }
}
