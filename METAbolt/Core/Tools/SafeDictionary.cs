/*
 * MEGAbolt Metaverse Client
 * Copyright(c) 2008-2014, www.metabolt.net (METAbolt)
 * Copyright(c) 2021, Sjofn, LLC
 * All rights reserved.
 *  
 * Radegast is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program.If not, see<https://www.gnu.org/licenses/>.
 */

using System.Collections.Generic;

namespace METAbolt
{
    public class SafeDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly object syncRoot = new object();
        private Dictionary<TKey, TValue> d = new Dictionary<TKey, TValue>();

        #region IDictionary<TKey,TValue> Members

        public void Add(TKey key, TValue value)
        {
            try
            {
                lock (syncRoot)
                {
                    d.Add(key, value);
                }
            }
            catch { ; }
        }

        public bool ContainsKey(TKey key)
        {
            lock (syncRoot)
            {
                return d.ContainsKey(key);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                lock (syncRoot)
                {
                    return d.Keys;
                }
            }
        }

        public bool Remove(TKey key)
        {
            lock (syncRoot)
            {
                try
                {
                    return d.Remove(key);
                }
                catch { return false; }
            }
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (syncRoot)
            {
                return d.TryGetValue(key, out value);
            }
        }

        public ICollection<TValue> Values
        {
            get
            {
                lock (syncRoot)
                {
                    return d.Values;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                try
                {
                    return d[key];
                }
                catch { return default(TValue); }
            }
            set
            {
                lock (syncRoot)
                {
                    d[key] = value;
                }
            }
        }

        #endregion

        #region ICollection<KeyValuePair<TKey,TValue>> Members

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            try
            {
                lock (syncRoot)
                {
                    ((ICollection<KeyValuePair<TKey, TValue>>)d).Add(item);
                }
            }
            catch { ; }
        }

        public void Clear()
        {
            try
            {
                lock (syncRoot)
                {
                    d.Clear();
                }
            }
            catch { ; }
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            lock (syncRoot)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)d).Contains(item);
            }
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int
        arrayIndex)
        {
            lock (syncRoot)
            {
                ((ICollection<KeyValuePair<TKey, TValue>>)d).CopyTo(array, arrayIndex);
            }
        }

        public int Count
        {
            get
            {
                lock (syncRoot)
                {
                    return d.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            lock (syncRoot)
            {
                try
                {
                    return ((ICollection<KeyValuePair<TKey,
                    TValue>>)d).Remove(item);
                }
                catch { return false; }
            }
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey,TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            lock (syncRoot)
            {
                return ((ICollection<KeyValuePair<TKey, TValue>>)d).GetEnumerator();
            }
        }

        #endregion

        #region IEnumerable Members

        System.Collections.IEnumerator
        System.Collections.IEnumerable.GetEnumerator()
        {
            lock (syncRoot)
            {
                return ((System.Collections.IEnumerable)d).GetEnumerator();
            }
        }

        #endregion
    }

}
