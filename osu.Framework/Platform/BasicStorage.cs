// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using SQLite;

namespace osu.Framework.Platform
{
    public abstract class BasicStorage
    {
        public string BaseName { get; set; }
    
        protected BasicStorage(string baseName)
        {
            BaseName = baseName;
        }

        public abstract Stream GetStream(string path, FileAccess mode = FileAccess.Read);
        public abstract SQLiteConnection GetDatabase(string name);
    }
}