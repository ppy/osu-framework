// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace osu.Framework.Model
{
    public class PropertyDescription : INotifyPropertyChanged
    {
        private string propertyName;

        /// <summary>
        /// Name of the property that should be referenced by this description.
        /// </summary>
        public string PropertyName
        {
            get
            {
                return propertyName;
            }
            set
            {
                propertyName = value;
                notifyPropertyChanged();
            }
        }

        private IComparer customSort;

        /// <summary>
        /// Custom comparer for sorting properties.
        /// </summary>
        public IComparer CustomSort
        {
            get
            {
                return customSort;
            }
            set
            {
                customSort = value;
                notifyPropertyChanged();
            }
        }

        private bool compareDescending;

        /// <summary>
        /// Defines if elements should be compared in descending order (i.e. greater element before lower).
        /// </summary>
        public bool CompareDescending
        {
            get
            {
                return compareDescending;
            }
            set
            {
                compareDescending = value;
                notifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Gets value of an object property referenced by this description.
        /// </summary>
        /// <param name="item">Object to obtain property value from.</param>
        /// <returns>Value of the object property referenced by this description.</returns>
        public object GetPropertyValue(object item)
        {
            return item.GetType().GetProperty(PropertyName).GetValue(item);
        }

        /// <summary>
        /// Delegate to converter an object property referenced by this description to string.
        /// </summary>
        /// <param name="item">Object to obtain property value from.</param>
        /// <returns>Item converted to string.</returns>
        public delegate string PropertyStringConverter(object item);

        /// <summary>
        /// Defines converter to convert an object property referenced by this description to string.
        /// </summary>
        /// <seealso cref="GetPropertyStringValue(object)"/>
        public PropertyStringConverter StringConverter;

        /// <summary>
        /// Gets a string value of an object property referenced by this description.
        /// </summary>
        /// <param name="item">Object to obtain property value from.</param>
        /// <returns>
        /// If StringConverter is defined, item converted to string using such converter;
        /// otherwise item string value as return by ToString() method.
        /// </returns>
        public string GetPropertyStringValue(object item)
        {
            if (StringConverter == null)
                return GetPropertyValue(item).ToString();
            return StringConverter.Invoke(item);
        }

        private void notifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
