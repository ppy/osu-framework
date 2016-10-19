// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using SQLite.Net;
using System.IO;
using osu.Framework.Platform;
using SQLite.Net.Platform.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Win32;

namespace osu.Framework.Desktop.Platform
{
    public abstract class DesktopStorage : BasicStorage
    {
        public DesktopStorage(string baseName) : base(baseName)
        {
        }
        
        protected abstract string BasePath { get; }
        
        public override Stream GetStream(string path, FileAccess mode = FileAccess.Read)
        {
            path = Path.Combine(BasePath, path);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            return File.Open(path, FileMode.OpenOrCreate, mode);
        }
        
        public override SQLiteConnection GetDatabase(string name)
        {
            Directory.CreateDirectory(BasePath);
            ISQLitePlatform platform;
            if (RuntimeInfo.IsWindows)
                platform = new SQLitePlatformWin32();
            else
                platform = new SQLitePlatformGeneric();
            return new SQLiteConnection(platform, Path.Combine(BasePath, $@"{name}.db"));
        }
    }
}