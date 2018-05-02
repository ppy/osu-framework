// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;

namespace osu.Framework.IO.Stores
{
    public class CachedResourceStore<T> : ResourceStore<T>
    {
        private readonly Dictionary<string, T> cache = new Dictionary<string, T>();

        /// <summary>
        /// Initializes a resource store with no stores.
        /// </summary>
        public CachedResourceStore()
        {
        }

        /// <summary>
        /// Initializes a resource store with a single store.
        /// </summary>
        /// <param name="stores">A collection of stores to add.</param>
        public CachedResourceStore(IResourceStore<T>[] stores)
            : base(stores)
        {
        }

        /// <summary>
        /// Initializes a resource store with a collection of stores.
        /// </summary>
        /// <param name="store">The store.</param>
        public CachedResourceStore(IResourceStore<T> store)
            : base(store)
        {
        }

        /// <summary>
        /// Notifies a bound delegate that the resource has changed.
        /// </summary>
        /// <param name="name">The resource that has changed.</param>
        protected override void NotifyChanged(string name)
        {
            cache.Remove(name);
            base.NotifyChanged(name);
        }

        /// <summary>
        /// Adds a resource store to this store.
        /// </summary>
        /// <param name="store">The store to add.</param>
        public override void AddStore(IResourceStore<T> store)
        {
            base.AddStore(store);

            if (store is ChangeableResourceStore<T> crm)
                crm.OnChanged += NotifyChanged;
        }

        /// <summary>
        /// Removes a store from this store.
        /// </summary>
        /// <param name="store">The store to remove.</param>
        public override void RemoveStore(IResourceStore<T> store)
        {
            base.RemoveStore(store);

            if (store is ChangeableResourceStore<T> crm)
                crm.OnChanged -= NotifyChanged;
        }

        /// <summary>
        /// Retrieves an object from the store.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The object.</returns>
        public override T Get(string name)
        {
            if (cache.TryGetValue(name, out T result))
                return result;

            result = base.Get(name);

            if (result != null)
                cache[name] = result;

            return result;
        }

        /// <summary>
        /// Releases a resource from the cache.
        /// </summary>
        /// <param name="name">The resource to release.</param>
        public void Release(string name)
        {
            cache.Remove(name);
        }

        /// <summary>
        /// Releases all resources stored in the cache.
        /// </summary>
        public void ResetCache()
        {
            cache.Clear();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            ResetCache();
        }
    }
}
