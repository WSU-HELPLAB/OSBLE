using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Collections;

namespace CreateNewAssignment
{
    public class SortedList<T> : IEnumerable where T : IComparable<T>
    {
        //this is not inherit because I cannot override the Add function
        private List<T> list = new List<T>();

        public int Count
        {
            get
            {
                return (list.Count);
            }
        }

        public T this[int index]
        {
            get
            {
                return list[index];
            }
        }

        public T GetPreviousItem(T item)
        {
            try
            {
                return list[IndexOf(item) - 1];
            }
            catch
            {
                return default(T);
            }
        }

        public T GetNextItem(T item)
        {
            try
            {
                if(item == null)
                {
                    return list[0];
                }
                else
                {
                return list[IndexOf(item) + 1];
                }
            }
            catch
            {
                return default(T);
            }
        }

        public int IndexOf(T item)
        {
            return findIndexOf(item, 0, Count - 1);
        }

        public int Update(T item)
        {
            Remove(item);
            return AddInOrder(item);
        }

        /// <summary>
        /// This finds where the item would be insert but does not actually insert it
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public int FindInsertionSpot(T item)
        {
            return findInsertionSpot(item, 0, Count - 1);
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public int AddInOrder(T item)
        {
            if (Count != 0)
            {
                int index = findInsertionSpot(item, 0, Count - 1);
                if (index == -1)
                {
                    index = 0;
                }
                list.Insert(index, item);
                return index;
            }
            else
            {
                list.Add(item);
                return 0;
            }
        }

        private int findIndexOf(T item, int lower, int upper)
        {
            if (lower > upper)
            {
                return -1;
            }

            int middleIndex = (upper - lower) / 2 + lower;
            int compared = item.CompareTo(list[middleIndex]);
            if (compared < 0)
            {
                return findIndexOf(item, lower, middleIndex - 1);
            }
            else if (compared > 0)
            {
                return findIndexOf(item, middleIndex + 1, upper);
            }
            else
            {
                return middleIndex;
            }
        }

        private int findInsertionSpot(T item, int lower, int upper)
        {
            //probably a nicer way to do this but the only way I could think of
            if (lower + 1 >= upper)
            {
                if (item.CompareTo(list[upper]) > 0)
                {
                    return upper + 1;
                }
                else if (item.CompareTo(list[lower]) < 0)
                {
                    return lower - 1;
                }
                else if (item.CompareTo(list[lower]) > 0)
                {
                    return upper;
                }
                else
                {
                    return lower;
                }
            }

            int middleIndex = (upper - lower) / 2 + lower;
            int compared = item.CompareTo(list[middleIndex]);
            if (compared < 0)
            {
                return findInsertionSpot(item, lower, middleIndex);
            }
            else if (compared > 0)
            {
                return findInsertionSpot(item, middleIndex, upper);
            }
            else
            {
                throw new Exception("Cannot insert two of the same items");
            }
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public void Remove(T item)
        {
            list.Remove(item);
        }

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion
    }
}
