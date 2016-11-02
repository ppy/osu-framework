// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Configuration;
using osu.Framework.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;

namespace osu.Framework.Lists
{
    public class CollectionView : ObservableCollection<object>, IBindable
    {
        private readonly PropertyComparer defaultGroupComparer = new PropertyComparer();
        private readonly PropertyComparer defaultSortComparer = new PropertyComparer();

        /// <summary>
        /// Defines custom comparer for grouping elements. Overrides GroupDescriptions.
        /// </summary>
        public IComparer CustomGroup { get; set; }

        /// <summary>
        /// Defines custom comparer for grouping elements properties.
        /// Overrides GroupDescriptions to calculate PropertiesEqualToPreviousItem.
        /// </summary>
        /// <seealso cref="PropertiesEqualToPreviousItem(int)"/>
        public PropertyComparer CustomGroupProperty { get; set; }

        /// <summary>
        /// Defines custom comparer for sorting elements. Overrides SortDescriptions.
        /// </summary>
        public IComparer CustomSort { get; set; }

        private ObservableCollection<PropertyDescription> groupDescriptions;

        /// <summary>
        /// Describes how the items in the collection are grouped by their properties in the view.
        /// </summary>
        public ObservableCollection<PropertyDescription> GroupDescriptions
        {
            get
            {
                return groupDescriptions;
            }
            set
            {
                if (groupDescriptions != null)
                    groupDescriptions.CollectionChanged -= descriptionsCollectionChanged;
                groupDescriptions = value;
                defaultGroupComparer.Descriptions = value;
                groupDescriptions.CollectionChanged += descriptionsCollectionChanged;
                Refresh();
            }
        }

        private ObservableCollection<PropertyDescription> sortDescriptions;

        /// <summary>
        /// Describes how the items in the collection are sorted by their properties in the view.
        /// </summary>
        public ObservableCollection<PropertyDescription> SortDescriptions
        {
            get
            {
                return sortDescriptions;
            }
            set
            {
                if (sortDescriptions != null)
                    sortDescriptions.CollectionChanged -= descriptionsCollectionChanged;
                sortDescriptions = value;
                defaultSortComparer.Descriptions = value;
                sortDescriptions.CollectionChanged += descriptionsCollectionChanged;
                Refresh();
            }
        }

        /// <summary>
        /// Indicates whether this view supports grouping.
        /// </summary>
        public bool CanGroup => (CustomGroup != null || GroupDescriptions.Count > 0);

        /// <summary>
        /// Indicates whether this view supports grouping by properties.
        /// </summary>
        public bool CanGroupByProperties => (CustomGroupProperty != null || GroupDescriptions.Count > 0);

        /// <summary>
        /// Indicates whether this view supports sorting after grouping.
        /// </summary>
        public bool CanSort => (CustomSort != null && SortDescriptions.Count > 0);

        private int currentIndex;

        /// <summary>
        /// Gets the index of the current item in the view.
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return currentIndex;
            }
            set
            {
                currentIndex = Math.Max(0, Math.Min(value, Items.Count - 1));
                TriggerValueChanged();
            }
        }

        /// <summary>
        /// Gets the current item in the view.
        /// </summary>
        public object CurrentItem
        {
            get
            {
                if (Items.Count == 0)
                    return null;
                return Items[CurrentIndex];
            }
            set
            {
                Parse(value);
            }
        }

        public string Description { get; set; }

        /// <summary>
        /// Occurs when the current item changes.
        /// </summary>
        public event EventHandler ValueChanged;

        public CollectionView()
        {
            GroupDescriptions = new ObservableCollection<PropertyDescription>();
            SortDescriptions = new ObservableCollection<PropertyDescription>();
        }

        public CollectionView(IEnumerable<object> items) : base()
        {
            GroupDescriptions = new ObservableCollection<PropertyDescription>();
            SortDescriptions = new ObservableCollection<PropertyDescription>();

            foreach (object item in items)
                Add(item);
        }

        public CollectionView(IList<object> items) : base()
        {
            GroupDescriptions = new ObservableCollection<PropertyDescription>();
            SortDescriptions = new ObservableCollection<PropertyDescription>();

            foreach (object item in items)
                Add(item);
        }

        /// <summary>
        /// Adds an object to the collection according to the GroupDescriptions and SortDescriptions.
        /// </summary>
        /// <param name="value">Not-null object to be added.</param>
        /// <returns>Index where the element was inserted.</returns>
        public new int Add(object value)
        {
            Debug.Assert(value != null);

            int index = getAdditionIndex(value);
            Insert(index, value);

            return index;
        }

        /// <summary>
        /// Group and sort elements according to the corresponding rules.
        /// </summary>
        public void Refresh()
        {
            for (int i = 0; i < Count; i++)
                Move(i, getAdditionIndex(Items[i], 0, i));
        }

        /// <summary>
        /// Calculate how many Group Properties has in common an indexed item with its previous one.
        /// </summary>
        /// <param name="i">Index of item to compare.</param>
        /// <returns>Amount of Group Properties in common with previous item.</returns>
        public int PropertiesEqualToPreviousItem(int i)
        {
            if (i <= 0 || !CanGroupByProperties)
                return 0;

            int eq = 0;
            foreach (PropertyDescription description in GroupDescriptions)
            {
                if (groupCompareProperty(Items[i - 1], Items[i], description) != 0)
                    return eq;
                eq++;
            }
            return eq;
        }

        /// <summary>
        /// Raises the value changed event.
        /// </summary>
        public void TriggerValueChanged() => ValueChanged?.Invoke(this, null);

        public bool Parse(object s)
        {
            if (s == null) return false;

            for (int i = 0; i < Items.Count; i++)
            {
                if (s.Equals(Items[i]))
                {
                    CurrentIndex = i;
                    return true;
                }
            }

            return false;
        }

        public void UnbindAll()
        {
            ValueChanged = null;
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            moveCurrentIndex(e);
            base.OnCollectionChanged(e);
        }

        /// <summary>
        /// Search all the list and gets the first index of the element larger than value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <returns>The index of the first element larger than value.</returns>
        private int getAdditionIndex(object value)
        {
            return getAdditionIndex(value, 0, Count);
        }

        /// <summary>
        /// Search items with index in [lo, hi) and gets the first index of the element larger than value.
        /// </summary>
        /// <param name="value">The value to search for.</param>
        /// <param name="lo">Index of the element to search first.</param>
        /// <param name="hi">Index of the element after the last to search.</param>
        /// <returns>The index of the first element larger than value.</returns>
        private int getAdditionIndex(object value, int lo, int hi)
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

        private void descriptionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove || e.Action == NotifyCollectionChangedAction.Reset || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (PropertyDescription description in e.OldItems)
                    description.PropertyChanged -= descriptionPropertyChanged;
            if (e.Action == NotifyCollectionChangedAction.Add || e.Action == NotifyCollectionChangedAction.Replace)
                foreach (PropertyDescription description in e.NewItems)
                    description.PropertyChanged += descriptionPropertyChanged;

            Refresh();
        }

        private void descriptionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            Refresh();
        }

        private int groupCompare(object x, object y)
        {
            return (CustomGroup != null) ? CustomGroup.Compare(x, y) : defaultGroupComparer.Compare(x, y);
        }

        private int groupCompareProperty(object x, object y, PropertyDescription description)
        {
            return (CustomGroupProperty != null) ? CustomGroupProperty.CompareProperty(x, y, description) : defaultGroupComparer.CompareProperty(x, y, description);
        }

        private int sortCompare(object x, object y)
        {
            return (CustomSort != null) ? CustomSort.Compare(x, y) : defaultSortComparer.Compare(x, y);
        }

        /// <summary>
        /// Calculates new position of the current index based on a change in the collection.
        /// </summary>
        /// <param name="e">Event.</param>
        private void moveCurrentIndex(NotifyCollectionChangedEventArgs e)
        {
            // Items resetted
            if (e.Action == NotifyCollectionChangedAction.Reset)
                CurrentIndex = 0;

            // Items replaced, SelectedItem was among those; notify value change
            if (e.Action == NotifyCollectionChangedAction.Replace &&
                e.NewStartingIndex <= CurrentIndex && CurrentIndex < e.OldStartingIndex + e.NewItems.Count)
                TriggerValueChanged();

            // Items added before SelectedItem; move forward index
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewStartingIndex <= CurrentIndex)
                CurrentIndex += e.NewItems.Count;

            // Items removed before SelectedItem
            if (e.Action == NotifyCollectionChangedAction.Remove && e.OldStartingIndex <= CurrentIndex)
            {
                // SelectedItem was among those; auto select item previous to removed items
                if (CurrentIndex < e.OldStartingIndex + e.OldItems.Count)
                    CurrentIndex = Math.Max(0, e.OldStartingIndex - 1);
                // Otherwise move back index
                else
                    CurrentIndex -= e.OldItems.Count;
            }

            // Items moved
            if (e.Action == NotifyCollectionChangedAction.Move)
            {
                // Items removed before SelectedItem
                if (e.OldStartingIndex <= CurrentIndex)
                {
                    // Selected item was among moved items; calculate new position
                    if (CurrentIndex < e.OldStartingIndex + e.OldItems.Count)
                        CurrentIndex = CurrentIndex - e.OldStartingIndex + e.NewStartingIndex;
                    // Items removed after SelectedItem; move back index
                    else if (CurrentIndex < e.NewStartingIndex)
                        CurrentIndex -= e.OldItems.Count;
                }
                // Items added before SelectedItem; move forward index
                else if (e.NewStartingIndex <= CurrentIndex)
                    CurrentIndex += e.OldItems.Count;
            }
        }
    }
}