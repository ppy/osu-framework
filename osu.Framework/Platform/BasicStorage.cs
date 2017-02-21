// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.IO.File;
using SQLite.Net;

namespace osu.Framework.Platform
{
    public abstract class BasicStorage
    {
        public string BaseName { get; set; }

        public FileSystemWatcher Watcher;
        public Action OnChanged;

        protected BasicStorage(string baseName)
        {
            BaseName = FileSafety.FilenameStrip(baseName);
        }

        public abstract void SetupWatcher(string subPath);

        public abstract bool Exists(string path);

        public abstract void Delete(string path);

        public abstract Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate);

        public abstract SQLiteConnection GetDatabase(string name);

        public abstract void DeleteDatabase(string name);

        public abstract void OpenInNativeExplorer();
    }
}

