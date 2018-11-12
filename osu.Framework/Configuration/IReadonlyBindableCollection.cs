// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;

namespace osu.Framework.Configuration
{
    public interface IReadonlyBindableCollection<T> : IReadOnlyCollection<T>
    {

        /// <summary>
        /// An event which is raised when an item gets added.
        /// </summary>
        event Action<T> ItemAdded;

        /// <summary>
        /// An event which is raised when an range of items get added.
        /// </summary>
        event Action<IEnumerable<T>> ItemRangeAdded;

        /// <summary>
        /// An event which is raised when an item gets removed.
        /// </summary>
        event Action<T> ItemRemoved;

        /// <summary>
        /// An event which is raised when all items are getting removed.
        /// </summary>
        event Action<IEnumerable<T>> ItemsCleared;

    }
}
