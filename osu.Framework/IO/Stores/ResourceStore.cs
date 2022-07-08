// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class ResourceStore<T> : IResourceStore<T>
        where T : class
    {
        private readonly Dictionary<string, Action> actionList = new Dictionary<string, Action>();

        private readonly List<IResourceStore<T>> stores = new List<IResourceStore<T>>();

        private readonly List<string> searchExtensions = new List<string>();

        /// <summary>
        /// Initializes a resource store with no stores.
        /// </summary>
        public ResourceStore()
        {
        }

        /// <summary>
        /// Initializes a resource store with a single store.
        /// </summary>
        /// <param name="store">The store.</param>
        public ResourceStore(IResourceStore<T> store = null)
        {
            if (store != null)
                AddStore(store);
        }

        /// <summary>
        /// Initializes a resource store with a collection of stores.
        /// </summary>
        /// <param name="stores">The collection of stores.</param>
        public ResourceStore(IResourceStore<T>[] stores)
        {
            foreach (var resourceStore in stores.Cast<ResourceStore<T>>())
                AddStore(resourceStore);
        }

        /// <summary>
        /// Notifies a bound delegate that the resource has changed.
        /// </summary>
        /// <param name="name">The resource that has changed.</param>
        protected virtual void NotifyChanged(string name)
        {
            if (!actionList.TryGetValue(name, out Action action))
                return;

            action?.Invoke();
        }

        /// <summary>
        /// Adds a nested resource store to this store.
        /// </summary>
        /// <param name="store">The store to add.</param>
        public virtual void AddStore(IResourceStore<T> store)
        {
            lock (stores)
                stores.Add(store);
        }

        /// <summary>
        /// Removes a store from this store.
        /// </summary>
        /// <param name="store">The store to remove.</param>
        public virtual void RemoveStore(IResourceStore<T> store)
        {
            lock (stores)
                stores.Remove(store);
        }

        public virtual async Task<T> GetAsync(string name, CancellationToken cancellationToken = default)
        {
            if (name == null)
                return null;

            var filenames = GetFilenames(name);

            foreach (IResourceStore<T> store in getStores())
            {
                foreach (string f in filenames)
                {
                    T result = await store.GetAsync(f, cancellationToken).ConfigureAwait(false);
                    if (result != null)
                        return result;
                }
            }

            return default;
        }

        /// <summary>
        /// Retrieves an object from the store.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <returns>The object.</returns>
        public virtual T Get(string name)
        {
            if (name == null)
                return null;

            var filenames = GetFilenames(name);

            foreach (IResourceStore<T> store in getStores())
            {
                foreach (string f in filenames)
                {
                    T result = store.Get(f);
                    if (result != null)
                        return result;
                }
            }

            return default;
        }

        public Stream GetStream(string name)
        {
            if (name == null)
                return null;

            var filenames = GetFilenames(name);

            foreach (IResourceStore<T> store in getStores())
            {
                foreach (string f in filenames)
                {
                    var result = store.GetStream(f);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        protected virtual IEnumerable<string> GetFilenames(string name)
        {
            yield return name;

            //add file extension if it's missing.
            foreach (string ext in searchExtensions)
                yield return $@"{name}.{ext}";
        }

        /// <summary>
        /// Binds a reload function to an object held by the store.
        /// </summary>
        /// <param name="name">The name of the object.</param>
        /// <param name="onReload">The reload function to bind.</param>
        public void BindReload(string name, Action onReload)
        {
            if (onReload == null)
                return;

            // Check if there's already a reload action bound
            if (actionList.ContainsKey(name))
                throw new InvalidOperationException($"A reload delegate is already bound to the resource '{name}'.");

            actionList[name] = onReload;
        }

        /// <summary>
        /// Add a file extension to automatically append to any lookups on this store.
        /// </summary>
        public void AddExtension(string extension)
        {
            extension = extension.Trim('.');

            if (!searchExtensions.Contains(extension))
                searchExtensions.Add(extension);
        }

        public virtual IEnumerable<string> GetAvailableResources()
        {
            lock (stores) return stores.SelectMany(s => s.GetAvailableResources()).ExcludeSystemFileNames();
        }

        private IResourceStore<T>[] getStores()
        {
            lock (stores) return stores.ToArray();
        }

        #region IDisposable Support

        private bool isDisposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
                lock (stores) stores.ForEach(s => s.Dispose());
            }
        }

        #endregion
    }
}
