// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using osu.Framework.Development;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Logging;

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

        /// <summary>
        /// Gets a collection of string representations of the resources available in this store.
        /// </summary>
        /// <returns>String representations of the resources available.</returns>
        IEnumerable<string> GetAvailableResources();
    }

    internal static class ResourceStoreExtensions
    {
        /// <summary>
        /// Outputs a message to the log if a resource was not retrieved on a non-background thread.
        /// </summary>
        /// <param name="store">The store which the resources was retrieved from.</param>
        /// <param name="resourceName">The resource retrieved.</param>
        public static void LogIfNonBackgroundThread<T>(this IResourceStore<T> store, string resourceName)
        {
            if (!DebugUtils.LogPerformanceIssues)
                return;

            if (ThreadSafety.IsUpdateThread || ThreadSafety.IsDrawThread || ThreadSafety.IsAudioThread)
            {
                Logger.Log($"Resource {resourceName} was retrieved from a {store.GetType().ReadableName()} on a non-background thread.", LoggingTarget.Performance);
                Logger.Log(new StackTrace(1).ToString(), LoggingTarget.Performance, outputToListeners: false);
            }
        }
    }
}
