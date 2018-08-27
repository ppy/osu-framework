// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public interface IResourceStore<T> : IDisposable
    {
        /// <summary>
        /// Retrieves an object from the store.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The object.</returns>
        T Get(string name);

        /// <summary>
        /// Retrieves an object from the store asynchronously.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The object.</returns>
        Task<T> GetAsync(string name);

        Stream GetStream(string name);
    }
}
