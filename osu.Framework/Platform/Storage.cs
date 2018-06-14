// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.IO.File;

namespace osu.Framework.Platform
{
    public abstract class Storage
    {
        protected string BaseName { get; set; }

        protected string BasePath { get; set; }

        /// <summary>
        /// An optional path to be added after <see cref="BaseName"/>.
        /// </summary>
        protected string SubDirectory { get; set; } = string.Empty;

        protected Storage(string baseName)
        {
            BaseName = FileSafety.FilenameStrip(baseName);
            BasePath = LocateBasePath();
            if (BasePath == null)
                throw new NullReferenceException(nameof(BasePath));
        }

        /// <summary>
        /// Find the location which will be used as a root for this storage.
        /// This should usually be a platform-specific implementation.
        /// </summary>
        /// <returns></returns>
        protected abstract string LocateBasePath();

        /// <summary>
        /// Get a Storage-usable path for the provided path.
        /// </summary>
        /// <param name="path">An incomplete path, usually provided as user input.</param>
        /// <param name="createIfNotExisting">Create the path if it doesn't already exist.</param>
        /// <returns></returns>
        protected string GetUsablePathFor(string path, bool createIfNotExisting = false)
        {
            var resolvedPath = Path.Combine(BasePath, BaseName, SubDirectory, path);
            if (createIfNotExisting) Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath));
            return resolvedPath;
        }

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
        /// <returns>A list of directories in the path, relative to the path.</returns>
        public abstract string[] GetDirectories(string path);

        /// <summary>
        /// Retrieve a <see cref="Storage"/> for a contained directory.
        /// </summary>
        /// <param name="path">The subdirectory to use as a root.</param>
        /// <returns>A more specific storage.</returns>
        public Storage GetStorageForDirectory(string path)
        {
            var clone = (Storage)MemberwiseClone();
            clone.SubDirectory = path;
            return clone;
        }

        /// <summary>
        /// Retrieve a stream from an underlying file inside this storage.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="access">The access requirements.</param>
        /// <param name="mode">The mode in which the file should be opened.</param>
        /// <returns>A stream associated with the requested path.</returns>
        public abstract Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate);

        /// <summary>
        /// Retrieve an SQLite database connection string from within this storage.
        /// </summary>
        /// <param name="name">The name of the database.</param>
        /// <returns>An SQLite connection string.</returns>
        public abstract string GetDatabaseConnectionString(string name);

        /// <summary>
        /// Delete an SQLite database from within this storage.
        /// </summary>
        /// <param name="name">The name of the database to delete.</param>
        public abstract void DeleteDatabase(string name);

        /// <summary>
        /// Opens a native file browser window to the root path of this storage.
        /// </summary>
        public abstract void OpenInNativeExplorer();
    }
}
