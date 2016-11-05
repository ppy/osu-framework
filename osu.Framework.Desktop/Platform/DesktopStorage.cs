// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using SQLite.Net;
using System.IO;
using osu.Framework.Platform;
using SQLite.Net.Platform.Generic;
using SQLite.Net.Interop;
using SQLite.Net.Platform.Win32;
using System.Diagnostics;
using osu.Framework.Logging;

namespace osu.Framework.Desktop.Platform
{
    public class DesktopStorage : BasicStorage
    {
        public DesktopStorage(string baseName) : base(baseName)
        {
            //todo: this is obviously not the right way to do this.
            Logger.LogDirectory = Path.Combine(BasePath, @"logs");
        }

        protected virtual string BasePath => @"./"; //use current directory by default

        public override bool Exists(string path) => File.Exists(Path.Combine(BasePath, path));

        public override void Delete(string path) => File.Delete(Path.Combine(BasePath, path));
        
        public override void OpenInNativeExplorer()
        {
            Process.Start(BasePath);
        }

        public override Stream GetStream(string path, FileAccess mode = FileAccess.Read)
        {
            path = Path.Combine(BasePath, path);
            switch (mode)
            {
                case FileAccess.Read:
                    if (!File.Exists(path))
                        return null;
                    return File.Open(path, FileMode.Open, mode, FileShare.Read);
                default:
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    return File.Open(path, FileMode.OpenOrCreate, mode);
            }
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