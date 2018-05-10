// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;

namespace osu.Framework.IO.Stores
{
    public interface IResourceStore<out T> : IDisposable
    {
        /// <summary>
        /// Retrieves an object from the store.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The object.</returns>
        T Get(string name);

        Stream GetStream(string name);
    }
}
