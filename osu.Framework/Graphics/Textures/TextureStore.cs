// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using osu.Framework.IO.Stores;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Logging;

namespace osu.Framework.Graphics.Textures
{
    /// <summary>
    /// Provides drawable-ready <see cref="Texture"/>s sourced from any number of provided sources (via constructor parameter or <see cref="AddTextureSource"/>).
    /// </summary>
    public class TextureStore : ITextureStore
    {
        private readonly Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private readonly ResourceStore<TextureUpload> uploadStore = new ResourceStore<TextureUpload>();
        private readonly List<ITextureStore> nestedStores = new List<ITextureStore>();

        private readonly IRenderer renderer;
        private readonly TextureFilteringMode filteringMode;
        private readonly bool manualMipmaps;

        protected TextureAtlas Atlas;

        private const int max_atlas_size = 1024;

        /// <summary>
        /// Decides at what resolution multiple this <see cref="TextureStore"/> is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public readonly float ScaleAdjust;

        public TextureStore(IRenderer renderer, IResourceStore<TextureUpload> store = null, bool useAtlas = true, TextureFilteringMode filteringMode = TextureFilteringMode.Linear, bool manualMipmaps = false, float scaleAdjust = 2)
        {
            if (store != null)
                AddTextureSource(store);

            this.renderer = renderer;
            this.filteringMode = filteringMode;
            this.manualMipmaps = manualMipmaps;

            ScaleAdjust = scaleAdjust;

            if (useAtlas)
            {
                int size = Math.Min(max_atlas_size, renderer.MaxTextureSize);
                Atlas = new TextureAtlas(renderer, size, size, filteringMode: filteringMode, manualMipmaps: manualMipmaps);
            }
        }

        /// <summary>
        /// Adds a texture data lookup source to load <see cref="Texture"/>s with.
        /// </summary>
        /// <remarks>
        /// Lookup sources can be implemented easily using a <see cref="TextureLoaderStore"/> to provide the final <see cref="TextureUpload"/>.
        /// </remarks>
        /// <param name="store">The store to add.</param>
        public virtual void AddTextureSource(IResourceStore<TextureUpload> store) => uploadStore.AddStore(store);

        /// <summary>
        /// Removes a texture data lookup source.
        /// </summary>
        /// <param name="store">The store to remove.</param>
        public virtual void RemoveTextureStore(IResourceStore<TextureUpload> store) => uploadStore.RemoveStore(store);

        /// <summary>
        /// Adds a nested texture store to use during <see cref="Texture"/> lookup if not found in this store.
        /// </summary>
        /// <remarks>
        /// Of note, nested stores will use their own sources and not include any sources added via <see cref="AddTextureSource"/>.
        /// </remarks>
        /// <param name="store">The store to add.</param>
        public virtual void AddStore(ITextureStore store)
        {
            lock (nestedStores)
                nestedStores.Add(store);
        }

        /// <summary>
        /// Removes a nested texture store.
        /// </summary>
        /// <param name="store">The store to remove.</param>
        public virtual void RemoveStore(ITextureStore store)
        {
            lock (nestedStores)
                nestedStores.Remove(store);
        }

        private Texture loadRaw(TextureUpload upload, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (upload == null) return null;

            Texture tex = null;

            if (Atlas != null)
            {
                if ((tex = Atlas.Add(upload.Width, upload.Height, wrapModeS, wrapModeT)) == null)
                {
                    Logger.Log(
                        $"Texture requested ({upload.Width}x{upload.Height}) which exceeds {nameof(TextureStore)}'s atlas size ({max_atlas_size}x{max_atlas_size}) - bypassing atlasing. Consider using {nameof(LargeTextureStore)}.",
                        LoggingTarget.Performance);
                }
            }

            tex ??= renderer.CreateTexture(upload.Width, upload.Height, manualMipmaps, filteringMode, wrapModeS, wrapModeT);
            tex.ScaleAdjust = ScaleAdjust;
            tex.SetData(upload);

            return tex;
        }

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The texture.</returns>
        public Task<Texture> GetAsync(string name, CancellationToken cancellationToken) => GetAsync(name, default, default, cancellationToken);

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The texture.</returns>
        public Task<Texture> GetAsync(string name, WrapMode wrapModeT, WrapMode wrapModeS, CancellationToken cancellationToken = default) =>
            Task.Run(() => Get(name, wrapModeS, wrapModeT), cancellationToken); // TODO: best effort. need to re-think textureCache data structure to fix this.

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <returns>The texture.</returns>
        public Texture Get(string name) => Get(name, default, default);

        private readonly Dictionary<string, Task> retrievalCompletionSources = new Dictionary<string, Task>();

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="wrapModeS">The texture wrap mode in horizontal direction.</param>
        /// <param name="wrapModeT">The texture wrap mode in vertical direction.</param>
        /// <returns>The texture.</returns>
        public virtual Texture Get(string name, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            var texture = get(name, wrapModeS, wrapModeT);

            if (texture == null)
            {
                lock (nestedStores)
                {
                    foreach (var nested in nestedStores)
                    {
                        if ((texture = nested.Get(name, wrapModeS, wrapModeT)) != null)
                            break;
                    }
                }
            }

            return texture;
        }

        public Stream GetStream(string name)
        {
            var stream = uploadStore.GetStream(name);

            if (stream == null)
            {
                lock (nestedStores)
                {
                    foreach (var nested in nestedStores)
                    {
                        if ((stream = nested.GetStream(name)) != null)
                            break;
                    }
                }
            }

            return stream;
        }

        public IEnumerable<string> GetAvailableResources()
        {
            lock (nestedStores)
                return uploadStore.GetAvailableResources().Concat(nestedStores.SelectMany(s => s.GetAvailableResources()).ExcludeSystemFileNames()).ToArray();
        }

        private Texture get(string name, WrapMode wrapModeS, WrapMode wrapModeT)
        {
            if (string.IsNullOrEmpty(name)) return null;

            string key = $"{name}:wrap-{(int)wrapModeS}-{(int)wrapModeT}";

            TaskCompletionSource<Texture> tcs = null;
            Task task;

            lock (retrievalCompletionSources)
            {
                // Check if the texture exists in the cache.
                if (TryGetCached(key, out var cached))
                    return cached;

                // check if an existing lookup was already started for this key.
                if (!retrievalCompletionSources.TryGetValue(key, out task))
                    // if not, take responsibility for the lookup.
                    retrievalCompletionSources[key] = (tcs = new TaskCompletionSource<Texture>()).Task;
            }

            // handle the case where a lookup is already in progress.
            if (task != null)
            {
                task.WaitSafely();

                // always perform re-lookups through TryGetCached (see LargeTextureStore which has a custom implementation of this where it matters).
                if (TryGetCached(key, out var cached))
                    return cached;

                return null;
            }

            this.LogIfNonBackgroundThread(key);

            Texture tex = null;

            try
            {
                tex = loadRaw(uploadStore.Get(name), wrapModeS, wrapModeT);
                if (tex != null)
                    tex.LookupKey = key;

                return CacheAndReturnTexture(key, tex);
            }
            catch (TextureTooLargeForGLException)
            {
                Logger.Log($"Texture \"{name}\" exceeds the maximum size supported by this device ({renderer.MaxTextureSize}px).", level: LogLevel.Error);
            }
            finally
            {
                // notify other lookups waiting on the same name lookup.
                lock (retrievalCompletionSources)
                {
                    Debug.Assert(tcs != null);

                    tcs.SetResult(tex);
                    retrievalCompletionSources.Remove(key);
                }
            }

            return null;
        }

        /// <summary>
        /// Attempts to retrieve an existing cached texture.
        /// </summary>
        /// <param name="lookupKey">The lookup key that uniquely identifies textures in the cache.</param>
        /// <param name="texture">The returned texture. Null if the texture did not exist in the cache.</param>
        /// <returns>Whether a cached texture was retrieved.</returns>
        protected virtual bool TryGetCached([NotNull] string lookupKey, [CanBeNull] out Texture texture)
        {
            lock (textureCache)
                return textureCache.TryGetValue(lookupKey, out texture);
        }

        /// <summary>
        /// Caches and returns the given texture.
        /// </summary>
        /// <param name="lookupKey">The lookup key that uniquely identifies textures in the cache.</param>
        /// <param name="texture">The texture to be cached and returned.</param>
        /// <returns>The texture to be returned.</returns>
        [CanBeNull]
        protected virtual Texture CacheAndReturnTexture([NotNull] string lookupKey, [CanBeNull] Texture texture)
        {
            lock (textureCache)
                return textureCache[lookupKey] = texture;
        }

        /// <summary>
        /// Disposes and removes a texture from the cache.
        /// </summary>
        /// <param name="texture">The texture to purge from the cache.</param>
        protected void Purge(Texture texture)
        {
            lock (textureCache)
            {
                if (textureCache.TryGetValue(texture.LookupKey, out var tex))
                {
                    // we are doing this locally as right now, Textures don't dispose the underlying texture (leaving it to GC finalizers).
                    // in the case of a purge operation we are pretty sure this is the intended behaviour.
                    if (tex != null)
                        new DisposableTexture(tex).Dispose();
                }

                textureCache.Remove(texture.LookupKey);
            }
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

                uploadStore.Dispose();
                lock (nestedStores) nestedStores.ForEach(s => s.Dispose());
            }
        }

        #endregion
    }
}
