using System.Collections.Generic;

namespace Magenheim.Core.DeepFractures;

internal static class DeepFractureDictionaryCompatibility
{
    internal static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary.ContainsKey(key))
            return false;

        dictionary.Add(key, value);
        return true;
    }
}
