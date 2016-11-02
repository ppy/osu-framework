// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace osu.Framework.Data
{
    public class PropertyGroupDescription : INotifyPropertyChanged
    {
        private string propertyName;
        public string PropertyName
        {
            get
            {
                return propertyName;
            }
            set
            {
                propertyName = value;
                NotifyPropertyChanged();
            }
        }

        private IComparer customSort;
        public IComparer CustomSort
        {
            get
            {
                return customSort;
            }
            set
            {
                customSort = value;
                NotifyPropertyChanged();
            }
        }

        private bool compareDescending;
        public bool CompareDescending
        {
            get
            {
                return compareDescending;
            }
            set
            {
                compareDescending = value;
                NotifyPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
