// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace osu.Framework.Graphics.Video
{
    /// <summary>
    /// Resolves the FFmpeg native functions required for video playback.
    ///
    /// If you want to support a different set of libraries, consider implementing the <see cref="IFunctionResolver"/> interface yourself.
    /// </summary>
    public sealed class FFmpegFunctionResolver : IFunctionResolver
    {
        // The order here matters because of inter-library dependencies.
        // avcodec and swscale depend on avutil, avformat depends on avcodec.
        private static readonly string[] libraries = ["avutil", "avcodec", "swscale", "avformat"];

        private readonly Dictionary<string, IntPtr> loadedLibraries = new Dictionary<string, IntPtr>(libraries.Length);

        /// <summary>
        /// Load the default set of FFmpeg libraries.
        /// </summary>
        /// <exception cref="DllNotFoundException">If one of the libraries cannot be found in the DLL directory.</exception>
        /// <exception cref="BadImageFormatException">If one of the library files is invalid.</exception>
        public FFmpegFunctionResolver()
        {
            foreach (string library in libraries)
                loadLibrary(library);
        }

        private void loadLibrary(string libraryName)
        {
            string libraryFileName = getLibraryFileName(libraryName);
            IntPtr handle = NativeLibrary.Load(libraryFileName, RuntimeInfo.EntryAssembly, DllImportSearchPath.UseDllDirectoryForDependencies);

            loadedLibraries.Add(libraryName, handle);
        }

        public T GetFunctionDelegate<T>(string libraryName, string functionName, bool throwOnError = true)
        {
            if (!loadedLibraries.TryGetValue(libraryName, out IntPtr libraryHandle))
                throw new InvalidOperationException($"Required library {getLibraryFileName(libraryName)} was not loaded on initialization.");

            IntPtr funcPtr = NativeLibrary.GetExport(libraryHandle, functionName);
            return Marshal.GetDelegateForFunctionPointer<T>(funcPtr);
        }

        private string getLibraryFileName(string libraryName)
        {
            int version = ffmpeg.LibraryVersionMap[libraryName];

            switch (RuntimeInfo.OS)
            {
                case RuntimeInfo.Platform.Windows:
                    return $"{libraryName}-{version}.dll";

                case RuntimeInfo.Platform.Linux:
                    return $"lib{libraryName}.so.{version}";

                case RuntimeInfo.Platform.macOS:
                    return $"lib{libraryName}.{version}.dylib";

                default:
                    throw new PlatformNotSupportedException();
            }
        }
    }
}
