// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.OpenGL;
using osu.Framework.Graphics.OpenGL.Textures;
using osu.Framework.IO.Stores;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osuTK.Graphics.ES30;

namespace osu.Framework.Graphics.Textures
{
    public class TextureStore : ResourceStore<TextureUpload>
    {
        private readonly Dictionary<string, Texture> textureCache = new Dictionary<string, Texture>();

        private readonly All filteringMode;
        private readonly bool manualMipmaps;

        protected TextureAtlas Atlas;

        private const int max_atlas_size = 1024;

        /// <summary>
        /// Decides at what resolution multiple this <see cref="TextureStore"/> is providing sprites at.
        /// ie. if we are providing high resolution (at 2x the resolution of standard 1366x768) sprites this should be 2.
        /// </summary>
        public readonly float ScaleAdjust;

        public TextureStore(IResourceStore<TextureUpload> store = null, bool useAtlas = true, All filteringMode = All.Linear, bool manualMipmaps = false, float scaleAdjust = 2)
            : base(store)
        {
            this.filteringMode = filteringMode;
            this.manualMipmaps = manualMipmaps;

            ScaleAdjust = scaleAdjust;

            AddExtension(@"png");
            AddExtension(@"jpg");

            if (useAtlas)
            {
                int size = Math.Min(max_atlas_size, GLWrapper.MaxTextureSize);
                Atlas = new TextureAtlas(size, size, filteringMode: filteringMode, manualMipmaps: manualMipmaps);
            }
        }

        private Texture getTexture(string name, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None) => loadRaw(base.Get(name), wrapModeS, wrapModeT);

        private Texture loadRaw(TextureUpload upload, WrapMode wrapModeS = WrapMode.None, WrapMode wrapModeT = WrapMode.None)
        {
            if (upload == null) return null;

            TextureGL glTexture = null;

            if (Atlas != null)
            {
                if ((glTexture = Atlas.Add(upload.Width, upload.Height, wrapModeS, wrapModeT)) == null)
                {
                    Logger.Log(
                        $"Texture requested ({upload.Width}x{upload.Height}) which exceeds {nameof(TextureStore)}'s atlas size ({max_atlas_size}x{max_atlas_size}) - bypassing atlasing. Consider using {nameof(LargeTextureStore)}.",
                        LoggingTarget.Performance);
                }
            }

            glTexture ??= new TextureGLSingle(upload.Width, upload.Height, manualMipmaps, filteringMode, wrapModeS, wrapModeT);

            Texture tex = new Texture(glTexture) { ScaleAdjust = ScaleAdjust };
            tex.SetData(upload);

            return tex;
        }

        /// <summary>
        /// Retrieves a texture from the store and adds it to the atlas.
        /// </summary>
        /// <param name="name">The name of the texture.</param>
        /// <param name="cancellationToken">A cancellation token.</param>
        /// <returns>The texture.</returns>
        public new Task<Texture> GetAsync(string name, CancellationToken cancellationToken) => GetAsync(name, default, default, cancellationToken);

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
        public new Texture Get(string name) => Get(name, default, default);

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
                tex = getTexture(name, wrapModeS, wrapModeT);
                if (tex != null)
                    tex.LookupKey = key;

                return CacheAndReturnTexture(key, tex);
            }
            catch (TextureTooLargeForGLException)
            {
                Logger.Log($"Texture \"{name}\" exceeds the maximum size supported by this device ({GLWrapper.MaxTextureSize}px).", level: LogLevel.Error);
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
                    tex?.TextureGL?.Dispose();
                    tex?.Dispose();
                }

                textureCache.Remove(texture.LookupKey);
            }
        }
    }
}
