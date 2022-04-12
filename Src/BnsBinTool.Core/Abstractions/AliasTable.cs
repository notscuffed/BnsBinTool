using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BnsBinTool.Core.DataStructs;

namespace BnsBinTool.Core.Abstractions
{
    public class AliasTable<T> : IDictionary<string, T>, IDictionary<Ref, T>
        where T : IRecord, IHaveAlias
    {
        private readonly Dictionary<string, T> _aliasTable = new();
        private readonly SortedDictionary<Ref, T> _refTable = new(RefComparer.Instance);

        ICollection<string> IDictionary<string, T>.Keys => _aliasTable.Keys;
        ICollection<Ref> IDictionary<Ref, T>.Keys => _refTable.Keys;
        public ICollection<T> Values => _refTable.Values;
        public int Count => _aliasTable.Count;
        public bool IsReadOnly => false;

        public T this[string key]
        {
            get => _aliasTable[key];
            set
            {
                _aliasTable[key] = value;
                _refTable[value.Ref] = value;
            }
        }

        public T this[Ref key]
        {
            get => _refTable[key];
            set
            {
                _refTable[key] = value;
                _aliasTable[value.Alias] = value;

            }
        }
        
        public Ref LastRef()
        {
            return _refTable.Keys.Last();
        }

        public IEnumerable<KeyValuePair<Ref, T>> Refs => _refTable;
        public IEnumerable<KeyValuePair<string, T>> Aliases => _aliasTable;

        IEnumerator<KeyValuePair<Ref, T>> IEnumerable<KeyValuePair<Ref, T>>.GetEnumerator()
        {
            return _refTable.GetEnumerator();
        }

        IEnumerator<KeyValuePair<string, T>> IEnumerable<KeyValuePair<string, T>>.GetEnumerator()
        {
            return _aliasTable.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _refTable.GetEnumerator();
        }

        public void Add(KeyValuePair<string, T> item)
        {
            _aliasTable.Add(item.Key, item.Value);
            _refTable.Add(item.Value.Ref, item.Value);
        }

        public void Add(KeyValuePair<Ref, T> item)
        {
            _refTable.Add(item.Key, item.Value);
            _aliasTable.Add(item.Value.Alias, item.Value);
        }

        void ICollection<KeyValuePair<Ref, T>>.Clear()
        {
            _refTable.Clear();
            _aliasTable.Clear();
        }

        public bool Contains(KeyValuePair<Ref, T> item)
        {
            return _refTable.Contains(item);
        }

        public void CopyTo(KeyValuePair<Ref, T>[] array, int arrayIndex)
        {
            ((IDictionary<Ref, T>) _refTable).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<Ref, T> item)
        {
            if (_refTable.Remove(item.Key))
            {
                _aliasTable.Remove(item.Value.Alias);
                return true;
            }

            return false;
        }

        void ICollection<KeyValuePair<string, T>>.Clear()
        {
            _refTable.Clear();
            _aliasTable.Clear();
        }

        public bool Contains(KeyValuePair<string, T> item)
        {
            return _aliasTable.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, T>[] array, int arrayIndex)
        {
            ((IDictionary<string, T>) _aliasTable).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<string, T> item)
        {
            if (_aliasTable.Remove(item.Key))
            {
                _refTable.Remove(item.Value.Ref);
                return true;
            }

            return false;
        }

        public void Add(string key, T value)
        {
            _aliasTable.Add(key, value);
            _refTable.Add(value.Ref, value);
        }

        public bool ContainsKey(string key)
        {
            return _aliasTable.ContainsKey(key);
        }

        public bool Remove(string key)
        {
            if (_aliasTable.TryGetValue(key, out var item) && _aliasTable.Remove(key))
            {
                _refTable.Remove(item.Ref);
                return true;
            }

            return false;
        }

        public bool TryGetValue(string key, out T value)
        {
            return _aliasTable.TryGetValue(key, out value);
        }

        public void Add(Ref key, T value)
        {
            _aliasTable.Add(value.Alias, value);
            _refTable.Add(key, value);
        }

        public bool ContainsKey(Ref key)
        {
            return _refTable.ContainsKey(key);
        }

        public bool Remove(Ref key)
        {
            if (_refTable.TryGetValue(key, out var item) && _refTable.Remove(key))
            {
                _aliasTable.Remove(item.Alias);
                return true;
            }

            return false;
        }

        public bool TryGetValue(Ref key, out T value)
        {
            return _refTable.TryGetValue(key, out value);
        }

        public void Clear()
        {
            _refTable.Clear();
            _aliasTable.Clear();
        }
    }
}