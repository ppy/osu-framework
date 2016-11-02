// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace osu.Framework.Lists
{
    public class ViewCollection<T> : ObservableCollection<T>
    {
        private readonly PropertyGroupComparer<T> defaultGroupComparer = new PropertyGroupComparer<T>();

        public IComparer<T> CustomSort { get; set; }
        public PropertyGroupComparer<T> CustomGroup { get; set; }

        private ObservableCollection<PropertyGroupDescription> groupDescriptions;
        public ObservableCollection<PropertyGroupDescription> GroupDescriptions
        {
            get
            {
                return groupDescriptions;
            }
            set
            {
                if (groupDescriptions != null)
                    groupDescriptions.CollectionChanged -= groupDescriptionsCollectionChanged;
                groupDescriptions = value;
                defaultGroupComparer.GroupDescriptions = value;
                groupDescriptions.CollectionChanged += groupDescriptionsCollectionChanged;
                Refresh();
            }
        }

        public bool IsGroupable => GroupDescriptions.Count > 0;

        public bool IsSortable { get; set; }

        public ViewCollection()
        {
             GroupDescriptions = new ObservableCollection<PropertyGroupDescription>();
        }

        public ViewCollection(IEnumerable<T> items) : base()
        {
            GroupDescriptions = new ObservableCollection<PropertyGroupDescription>();
            foreach (T item in items)
                Add(item);
        }

        public ViewCollection(IList<T> items) : base()
        {
            GroupDescriptions = new ObservableCollection<PropertyGroupDescription>();
            foreach (T item in items)
                Add(item);
        }

        public new int Add(T value)
        {
            Debug.Assert(value != null);
            Debug.Assert(value is T);

            int index = getAdditionIndex(value);
            Insert(index, value);

            return index;
        }

        public void Refresh()
        {
            for (int i = 0; i < Count; i++)
            {
                Move(i, getAdditionIndex(Items[i], 0, i));
            }
        }

        /// <summary>
        /// Calculates how many Group Properties has in common an indexed item with its previous one.
        /// </summary>
        /// <param name="i">Index of item to compare.</param>
        /// <returns>Amount of Group Properties in common with previous item.</returns>
        public int PropertiesEqualToPreviousItem(int i)
        {
            if (i <= 0 || !IsGroupable)
                return 0;

            int eq = 0;
            foreach (PropertyGroupDescription description in GroupDescriptions)
            {
                if (groupCompareProperty(Items[i - 1], Items[i], description) != 0)
                    return eq;
                eq++;
            }
            return eq;
        }

        private int getAdditionIndex(T value)
        {
            return getAdditionIndex(value, 0, Count);
        }

        private int getAdditionIndex(T value, int lo, int hi)
        {
            while (lo < hi)
            {
                int mi = (lo + hi) / 2;
                int comparison = groupCompare(value, Items[mi]);
                if (comparison < 0 || (comparison == 0 && sortCompare(value, Items[mi]) < 0))
                    hi = mi;
                else
                    lo = mi + 1;
            }
            return lo;
        }

        private void groupDescriptionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (PropertyGroupDescription description in e.OldItems)
                    description.PropertyChanged -= groupDescriptionPropertyChanged;
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (PropertyGroupDescription description in e.NewItems)
                    description.PropertyChanged += groupDescriptionPropertyChanged;
            Refresh();
        }

        private void groupDescriptionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Refresh();
        }

        private int groupCompare(T x, T y)
        {
            return (CustomGroup != null) ? CustomGroup.Compare(x, y) : defaultGroupComparer.Compare(x, y);
        }

        private int groupCompareProperty(T x, T y, PropertyGroupDescription description)
        {
            return (CustomGroup != null) ? CustomGroup.CompareProperty(x, y, description) : defaultGroupComparer.CompareProperty(x, y, description);
        }

        private int sortCompare(T x, T y)
        {
            if (!IsSortable)
                return 0;
            if (CustomSort != null)
                return CustomSort.Compare(x, y);
            return (x is IComparable) ? (x as IComparable).CompareTo(y) : 0;
        }
    }
}