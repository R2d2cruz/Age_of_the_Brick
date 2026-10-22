using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable key-value pair used by SerializableDictionary.
/// </summary>
[Serializable]
public struct SerializableKeyValuePair<TKey, TValue>
{
    public TKey Key;
    public TValue Value;
}

/// <summary>
/// Unity-serializable dictionary wrapper.
/// Stores data as a serializable list while exposing
/// a dictionary-like API at runtime.
/// </summary>
[Serializable]
public class SerializableDictionary<TKey, TValue> :
    IReadOnlyDictionary<TKey, TValue>,
    ISerializationCallbackReceiver
{
    [SerializeField]
    private List<SerializableKeyValuePair<TKey, TValue>> entries = new();

    private Dictionary<TKey, TValue> dictionaryCache;

    /// <summary>
    /// Rebuilds the runtime dictionary cache from serialized entries.
    /// </summary>
    private void RebuildCache()
    {
        dictionaryCache = new Dictionary<TKey, TValue>();

        foreach (var entry in entries)
        {
            dictionaryCache[entry.Key] = entry.Value;
        }
    }

    /// <summary>
    /// Provides access to the runtime dictionary cache.
    /// </summary>
    private Dictionary<TKey, TValue> RuntimeDictionary
    {
        get
        {
            if (dictionaryCache == null)
            {
                RebuildCache();
            }

            return dictionaryCache;
        }
    }

    /// <summary>
    /// Accesses a value by key.
    /// </summary>
    public TValue this[TKey key] => RuntimeDictionary[key];

    /// <summary>
    /// Enumerates all keys.
    /// </summary>
    public IEnumerable<TKey> Keys => RuntimeDictionary.Keys;

    /// <summary>
    /// Enumerates all values.
    /// </summary>
    public IEnumerable<TValue> Values => RuntimeDictionary.Values;

    /// <summary>
    /// Total number of entries.
    /// </summary>
    public int Count => RuntimeDictionary.Count;

    /// <summary>
    /// Determines whether the specified key exists.
    /// </summary>
    public bool ContainsKey(TKey key)
    {
        return RuntimeDictionary.ContainsKey(key);
    }

    /// <summary>
    /// Attempts to retrieve a value by key.
    /// </summary>
    public bool TryGetValue(TKey key, out TValue value)
    {
        return RuntimeDictionary.TryGetValue(key, out value);
    }

    /// <summary>
    /// Returns a dictionary enumerator.
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        return RuntimeDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    /// <summary>
    /// Implicit conversion to Dictionary.
    /// </summary>
    public static implicit operator Dictionary<TKey, TValue>(
        SerializableDictionary<TKey, TValue> source)
    {
        return source.RuntimeDictionary;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        dictionaryCache = null;
    }
}


[Serializable]
public class ResourceCostDictionary
    : SerializableDictionary<ResourceType, int>
{
}