using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class SerializableDictionary
{
}

[System.Serializable]
public class SerializableDictionary<KeyType, ValueType> : SerializableDictionary, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<KeyType> _keys = new List<KeyType>();
    [SerializeField]
    private List<ValueType> _values = new List<ValueType>();
    private Dictionary<KeyType, ValueType> _backingDictionary = new Dictionary<KeyType, ValueType>();
    private HashSet<int> _duplicateKeys = new HashSet<int>();
    [SerializeField]
    [HideInInspector]
    private bool _opened;
    [SerializeField]
    [HideInInspector]
    private List<bool> _openedKVPairs = new List<bool>();

    public void OnAfterDeserialize()
    {
        _duplicateKeys.Clear();
        _backingDictionary.Clear();
        for (int idx = 0; idx < _keys.Count; idx++)
        {
            if (_backingDictionary.ContainsKey(_keys[idx]))
            {
                _duplicateKeys.Add(idx);
                continue;
            }
            _backingDictionary.Add(_keys[idx], _values[idx]);
        }
    }

    public void OnBeforeSerialize()
    {
        if (Count == _backingDictionary.Count)
        {
            return;
        }
        KeyType[] dictionaryKeys = new KeyType[_backingDictionary.Count];
        ValueType[] dictionaryValues = new ValueType[_backingDictionary.Count];
        List<KeyType> tempKeys = new List<KeyType>();
        List<ValueType> tempValues = new List<ValueType>();
        int dictionaryIdx = 0;
        foreach (KeyValuePair<KeyType, ValueType> kvPair in _backingDictionary)
        {
            dictionaryKeys[dictionaryIdx] = kvPair.Key;
            dictionaryValues[dictionaryIdx] = kvPair.Value;
        }
        dictionaryIdx = 0;
        for (int idx = 0; idx < _keys.Count; idx++)
        {
            if (_duplicateKeys.Contains(idx))
            {
                tempKeys.Add(_keys[idx]);
                tempValues.Add(_values[idx]);
                continue;
            }
            if (dictionaryIdx >= _backingDictionary.Count ||
                !_backingDictionary.ContainsKey(_keys[idx]))
            {
                continue;
            }
            tempKeys.Add(dictionaryKeys[dictionaryIdx]);
            tempValues.Add(dictionaryValues[dictionaryIdx]);
            dictionaryIdx++;
        }
        _keys.Clear();
        _values.Clear();
        for (int idx = 0; idx < tempKeys.Count; idx++)
        {
            _keys.Add(tempKeys[idx]);
            _values.Add(tempValues[idx]);
        }
    }

    public Dictionary<KeyType, ValueType> Dictionary
    {
        get { return _backingDictionary; }
    }
    public int Count
    {
        get { return _keys.Count - _duplicateKeys.Count; }
    }
}