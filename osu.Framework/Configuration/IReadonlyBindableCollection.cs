// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Configuration
{
    public interface IReadonlyBindableCollection<out T> : IReadOnlyCollection<T>
    {
        /// <summary>
        /// An event which is raised when an range of items get added.
        /// </summary>
        event Action<IEnumerable<T>> ItemsAdded;

        /// <summary>
        /// An event which is raised when items get removed.
        /// </summary>
        event Action<IEnumerable<T>> ItemsRemoved;
    }
}
