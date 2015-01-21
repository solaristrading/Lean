﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License"); 
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections;
using System.Collections.Generic;

namespace QuantConnect.Indicators
{
    /// <summary>
    ///     This is a window that allows for list access semantics,
    ///     where this[0] refers to the most recent item in the
    ///     window and this[Count-1] refers to the last item in the window
    /// </summary>
    /// <typeparam name="T">The type of data in the window</typeparam>
    public class RollingWindow<T> : IReadOnlyWindow<T>
    {
        // the backing list object used to hold the data
        private readonly List<T> _list;
        // lock used for modifying, also while getting enumerator to avoid mutation while enumerating
        private readonly object _lock = new object();
        // the most recently removed item from the window (fell off the back)
        private T _mostRecentlyRemoved;
        // the total number of samples taken by this indicator
        private decimal _samples;
        // used to locate the last item in the window as an indexer into the _list
        private int _tail;

        /// <summary>
        ///     Initializes a new instance of the RollwingWindow class with the specified window size.
        /// </summary>
        /// <param name="size">The number of items to hold in the window</param>
        public RollingWindow(int size)
        {
            if (size < 1)
            {
                throw new ArgumentException("RollingWindow must have size of at least 1.", "size");
            }
            _list = new List<T>(size);
        }

        /// <summary>
        ///     Gets the size of this window
        /// </summary>
        public int Size
        {
            get { return _list.Capacity; }
        }

        /// <summary>
        ///     Gets the current number of elements in this window
        /// </summary>
        public int Count
        {
            get { return _list.Count; }
        }

        /// <summary>
        ///     Gets the number of samples that have been added to this window over its lifetime
        /// </summary>
        public decimal Samples
        {
            get { return _samples; }
        }

        /// <summary>
        ///     Gets the most recently removed item from the window. This is the
        ///     piece of data that just 'fell off' as a result of the most recent
        ///     add. If no items have been removed, this will throw an exception.
        /// </summary>
        public T MostRecentlyRemoved
        {
            get
            {
                if (!IsReady)
                {
                    throw new InvalidOperationException("No items have been removed yet!");
                }
                return _mostRecentlyRemoved;
            }
        }

        /// <summary>
        ///     Indexes into this window, where index 0 is the most recently
        ///     entered value
        /// </summary>
        /// <param name="i">the index, i</param>
        /// <returns>the ith most recent entry</returns>
        public T this[int i]
        {
            get
            {
                if (i >= Count)
                {
                    throw new ArgumentOutOfRangeException("i", i, string.Format("Must be between 0 and Count {{{0}}}", Count));
                }
                return _list[(Count + _tail - i - 1) % Count];
            }
            set
            {
                if (i >= Count)
                {
                    throw new ArgumentOutOfRangeException("i", i, string.Format("Must be between 0 and Count {{{0}}}", Count));
                }
                _list[(Count + _tail - i - 1) % Count] = value;
            }
        }

        /// <summary>
        ///     Gets a value indicating whether or not this window is ready, i.e,
        ///     it has been filled to its capacity and one has fallen off the back
        /// </summary>
        public bool IsReady
        {
            get { return Samples > Size; }
        }

        /// <summary>
        ///     Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        ///     A <see cref="T:System.Collections.Generic.IEnumerator`1" /> that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>1</filterpriority>
        public IEnumerator<T> GetEnumerator()
        {
            // we make a copy on purpose so the enumerator isn't tied 
            // to a mutable object, well it is still mutable but out of scope
            var temp = new List<T>(Count);
            lock (_lock)
            {
                for (int i = 0; i < Count; i++)
                {
                    temp.Add(this[i]);
                }
            }
            return temp.GetEnumerator();
        }

        /// <summary>
        ///     Returns an enumerator that iterates through a collection.
        /// </summary>
        /// <returns>
        ///     An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Adds an item to this window and shifts all other elements
        /// </summary>
        /// <param name="item">The item to be added</param>
        public void Add(T item)
        {
            lock (_lock)
            {
                _samples++;
                if (Size == Count)
                {
                    // keep track of what's the last element
                    // so we can reindex on this[ int ]
                    _mostRecentlyRemoved = _list[_tail];
                    _list[_tail] = item;
                    _tail = (_tail + 1) % Size;
                }
                else
                {
                    _list.Add(item);
                }
            }
        }

        /// <summary>
        ///     Clears this window of all data
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _samples = 0;
                _list.Clear();
            }
        }
    }
}