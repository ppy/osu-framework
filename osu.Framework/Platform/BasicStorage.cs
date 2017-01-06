﻿// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.IO;
using osu.Framework.IO.File;
using SQLite.Net;

namespace osu.Framework.Platform
{
    public abstract class BasicStorage
    {
        public string BaseName { get; set; }
    
        protected BasicStorage(string baseName)
        {
            BaseName = FileSafety.FilenameStrip(baseName);
        }

        public abstract bool Exists(string path);

        public abstract void Delete(string path);

        public abstract Stream GetStream(string path, FileAccess mode = FileAccess.Read);

        public abstract SQLiteConnection GetDatabase(string name);

        public abstract void OpenInNativeExplorer();
    }
}