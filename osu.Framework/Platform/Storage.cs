// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.IO.File;
using SQLite.Net;

namespace osu.Framework.Platform
{
    public abstract class Storage
    {
        protected string BaseName { get; set; }

        protected Storage(string baseName)
        {
            BaseName = FileSafety.FilenameStrip(baseName);
        }

        /// <summary>
        /// Check whether a file exists at the specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public abstract bool Exists(string path);

        /// <summary>
        /// Check whether a directory exists at the specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
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
        /// Retrieve a stream from an underlying file inside this storage.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <param name="access">The access requirements.</param>
        /// <param name="mode">The mode in which the file should be opened.</param>
        /// <returns>A stream associated with the requested path.</returns>
        public abstract Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate);

        /// <summary>
        /// Retrieve an SQLite database from within this storage.
        /// </summary>
        /// <param name="name">The name of the database.</param>
        /// <returns>An SQLite connection.</returns>
        public abstract SQLiteConnection GetDatabase(string name);

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
