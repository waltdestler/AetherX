using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tainicom.Aether.Physics2D.Common
{
    public class ObservableList<T> : IList<T>
    {
        #region Events 

        public event EventHandler<ObservableListArgs<T>> ItemAdded;
        public event EventHandler<ObservableListArgs<T>> ItemRemoved;

        private void RaiseEvent(EventHandler<ObservableListArgs<T>> eventHandler, T item, int index)
        {
            var eh = eventHandler;
            eh?.Invoke(this, new ObservableListArgs<T>(item, index));
        }

        #endregion

        private readonly List<T> list;

        public ObservableList()
        {
            list = new List<T>();
        }

        public ObservableList(IEnumerable<T> collection)
        {
            list = new List<T>();

            foreach (var item in collection)
            {
                this.Add(item);
            }
        }

        public T this[int index] { get => ((IList<T>)list)[index]; set => ((IList<T>)list)[index] = value; }

        public int Count => ((IList<T>)list).Count;

        public bool IsReadOnly => ((IList<T>)list).IsReadOnly;

        public bool Contains(T item)
        {
            return ((IList<T>)list).Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            ((IList<T>)list).CopyTo(array, arrayIndex);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IList<T>)list).GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return ((IList<T>)list).IndexOf(item);
        }

        public bool Remove(T item)
        {
            var index = list.IndexOf(item);

            if (index < 0)
                return false;

            RemoveAt(index);
            return true;
        }

        public void Clear()
        {
            for (var i = this.list.Count - 1; i >= 0; i--)
            {
                this.RemoveAt(i);
            }
        }

        #region Methods which alter the collection and raise events.

        public void Add(T item)
        {
            // store new index for event
            var index = list.Count;

            ((IList<T>)list).Add(item);

            // raise event
            RaiseEvent(ItemAdded, item, index);
        }

        public void Insert(int index, T item)
        {
            ((IList<T>)list).Insert(index, item);

            // raise event
            RaiseEvent(ItemAdded, item, index);
        }

        public void RemoveAt(int index)
        {
            // store item for event
            var item = list[index];

            ((IList<T>)list).RemoveAt(index);

            // raise event
            RaiseEvent(ItemRemoved, item, index);
        }

        #endregion
    }

    public class ObservableListArgs<T> : EventArgs
    {
        public ObservableListArgs(T item, int index)
        {
            Item = item;
            Index = index;
        }

        public T Item { get; }
        public int Index { get; }
    }
}
