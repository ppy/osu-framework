// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Diagnostics;
using System.IO;
using osu.Framework.IO.File;

namespace osu.Framework.Platform
{
    public class DesktopStorage : Storage
    {
        public DesktopStorage(string baseName)
            : base(baseName)
        {
        }

        protected override string LocateBasePath() => @"./"; //use current directory by default

        public override bool Exists(string path) => File.Exists(GetUsablePathFor(path));

        public override bool ExistsDirectory(string path) => Directory.Exists(GetUsablePathFor(path));

        public override void DeleteDirectory(string path)
        {
            path = GetUsablePathFor(path);

            // handles the case where the directory doesn't exist, which will throw a DirectoryNotFoundException.
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public override void Delete(string path) => FileSafety.FileDelete(GetUsablePathFor(path));

        public override string[] GetDirectories(string path) => Directory.GetDirectories(GetUsablePathFor(path));

        public override void OpenInNativeExplorer()
        {
            Process.Start(GetUsablePathFor(string.Empty));
        }

        public override Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
        {
            path = GetUsablePathFor(path, access != FileAccess.Read);

            if (string.IsNullOrEmpty(path))
                throw new ArgumentNullException(nameof(path));

            switch (access)
            {
                case FileAccess.Read:
                    if (!File.Exists(path)) return null;
                    return File.Open(path, FileMode.Open, access, FileShare.Read);
                default:
                    return File.Open(path, mode, access);
            }
        }

        public override string GetDatabaseConnectionString(string name)
        {
            return string.Concat("Data Source=", GetUsablePathFor($@"{name}.db", true));
        }

        public override void DeleteDatabase(string name) => Delete($@"{name}.db");
    }
}
