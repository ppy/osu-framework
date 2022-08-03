// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;

namespace osu.Framework.Platform
{
    public abstract class Storage
    {
        protected string BasePath { get; }

        protected Storage(string path, string subfolder = null)
        {
            static string filenameStrip(string entry)
            {
                foreach (char c in Path.GetInvalidFileNameChars())
                    entry = entry.Replace(c.ToString(), string.Empty);
                return entry;
            }

            BasePath = path;

            if (BasePath == null)
                throw new InvalidOperationException($"{nameof(BasePath)} not correctly initialized!");

            if (!string.IsNullOrEmpty(subfolder))
                BasePath = Path.Combine(BasePath, filenameStrip(subfolder));
        }

        /// <summary>
        /// Get a usable filesystem path for the provided incomplete path.
        /// </summary>
        /// <param name="path">An incomplete path, usually provided as user input.</param>
        /// <param name="createIfNotExisting">Create the path if it doesn't already exist.</param>
        /// <returns>A usable filesystem path.</returns>
        public abstract string GetFullPath(string path, bool createIfNotExisting = false);

        /// <summary>
        /// Check whether a file exists at the specified path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>Whether a file exists.</returns>
        public abstract bool Exists(string path);

        /// <summary>
        /// Check whether a directory exists at the specified path.
        /// </summary>
        /// <param name="path">The path to check.</param>
        /// <returns>Whether a directory exists.</returns>
        public abstract bool ExistsDirectory(string path);

        /// <summary>
        /// Delete a directory and all its contents recursively.
        /// </summary>
        /// <param name="path">The path of the directory to delete.</param>
        public abstract void DeleteDirectory(string path);

        /// <summary>
        /// Delete a file.
        /// </summary>
        /// <param name="path">The path of the file to delete.</param>
        public abstract void Delete(string path);

        /// <summary>
        /// Retrieve a list of directories at the specified path.
        /// </summary>
        /// <param name="path">The path to list.</param>
        /// <returns>A list of directories in the path, relative to the path of this storage.</returns>
        public abstract IEnumerable<string> GetDirectories(string path);

        /// <summary>
        /// Retrieve a list of files at the specified path.
        /// </summary>
        /// <param name="path">The path to list.</param>
        /// <param name="pattern">An optional search pattern. Accepts "*" wildcard.</param>
        /// <returns>A list of files in the path, relative to the path of this storage.</returns>
        public abstract IEnumerable<string> GetFiles(string path, string pattern = "*");

        /// <summary>
        /// Retrieve a <see cref="Storage"/> for a contained directory.
        /// Creates the path if not existing.
        /// </summary>
        /// <param name="path">The subdirectory to use as a root.</param>
        /// <returns>A more specific storage.</returns>
        public virtual Storage GetStorageForDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Must be non-null and not empty string", nameof(path));

            if (!path.EndsWith(Path.DirectorySeparatorChar))
                path += Path.DirectorySeparatorChar;

            // create non-existing path.
            string fullPath = GetFullPath(path, true);

            return (Storage)Activator.CreateInstance(GetType(), fullPath);
        }

        /// <summary>
        /// Move a file from one location to another. File must exist. Destination must not exist.
        /// </summary>
        /// <param name="from">The file path to move.</param>
        /// <param name="to">The destination path.</param>
        public abstract void Move(string from, string to);

        /// <summary>
        /// Create a new file on disk, using a temporary file to write to before moving to the final location to ensure a half-written file cannot exist at the specified location.
        /// </summary>
        /// <remarks>
        /// If the target file path already exists, it will be deleted before attempting to write a new version.
        /// </remarks>
        /// <param name="path">The path of the file to create or overwrite.</param>
        /// <returns>A stream associated with the requested path. Will only exist at the specified location after the stream is disposed.</returns>
        [Pure]
        public Stream CreateFileSafely(string path)
        {
            string temporaryPath = Path.Combine(Path.GetDirectoryName(path), $"_{Path.GetFileName(path)}_{Guid.NewGuid()}");

            return new SafeWriteStream(temporaryPath, path, this);
        }

        /// <summary>
        /// Retrieve a stream from an underlying file inside this storage.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="access">The access requirements.</param>
        /// <param name="mode">The mode in which the file should be opened.</param>
        /// <returns>A stream associated with the requested path.</returns>
        [Pure]
        public abstract Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate);

        /// <summary>
        /// Requests that a file be opened externally with an associated application, if available.
        /// </summary>
        /// <param name="filename">The relative path to the file which should be opened.</param>
        /// <returns>Whether the file was successfully opened.</returns>
        public abstract bool OpenFileExternally(string filename);

        /// <summary>
        /// Opens a native file browser window to the root path of this storage.
        /// </summary>
        /// <returns>Whether the storage was successfully presented.</returns>
        public bool PresentExternally() => OpenFileExternally(string.Empty);

        /// <summary>
        /// Requests to present a file externally in the platform's native file browser.
        /// </summary>
        /// <remarks>
        /// This will open the parent folder and, (if available) highlight the file.
        /// </remarks>
        /// <param name="filename">Relative path to the file.</param>
        /// <returns>Whether the file was successfully presented.</returns>
        public abstract bool PresentFileExternally(string filename);

        /// <summary>
        /// Uses a temporary file to ensure a file is written to completion before existing at its specified location.
        /// </summary>
        private class SafeWriteStream : FileStream
        {
            private readonly string temporaryPath;
            private readonly string finalPath;
            private readonly Storage storage;

            public SafeWriteStream(string temporaryPath, string finalPath, Storage storage)
                : base(storage.GetFullPath(temporaryPath, true), FileMode.Create, FileAccess.Write)
            {
                this.temporaryPath = temporaryPath;
                this.finalPath = finalPath;
                this.storage = storage;
            }

            private bool isDisposed;

            protected override void Dispose(bool disposing)
            {
                if (!isDisposed)
                {
                    // this was added to work around some hardware writing zeroes to a file
                    // before writing actual content, causing corrupt files to exist on disk.
                    // as of .NET 6, flushing is very expensive on macOS so this is limited to only Windows,
                    // but it may also be entirely unnecessary due to the temporary file copying performed on this class.
                    // see: https://github.com/ppy/osu-framework/issues/5231
                    if (RuntimeInfo.OS == RuntimeInfo.Platform.Windows)
                    {
                        try
                        {
                            Flush(true);
                        }
                        catch
                        {
                            // this may fail due to a lower level file access issue.
                            // we don't want to throw in disposal though.
                        }
                    }
                }

                base.Dispose(disposing);

                if (!isDisposed)
                {
                    storage.Delete(finalPath);
                    storage.Move(temporaryPath, finalPath);

                    isDisposed = true;
                }
            }
        }
    }
}
