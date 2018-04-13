// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.IO.Stores
{
    public class NamespacedResourceStore<T> : ResourceStore<T>
    {
        public string Namespace;

        /// <summary>
        /// Initializes a resource store with a single store.
        /// </summary>
        /// <param name="store">The store.</param>
        /// <param name="ns">The namespace to add.</param>
        public NamespacedResourceStore(IResourceStore<T> store, string ns)
            : base(store)
        {
            Namespace = ns;
        }

        protected override List<string> GetFilenames(string name)
        {
            return base.GetFilenames($@"{Namespace}/{name}");
        }
    }
}
