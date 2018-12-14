// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using Android.App;
using Android.OS;
using osu.Framework.Platform;

using Environment = Android.OS.Environment;

namespace osu.Framework.Android
{
    public class AndroidStorage : DesktopStorage
    {
        public AndroidStorage(string baseName, GameHost host)
            : base(baseName, host)
        {

        }

        //Dopilnuj, aby zamieniać ścieżki względne na bezwzględne

        protected override string LocateBasePath()
        {
            return Application.Context.GetExternalFilesDir("").ToString();
        }

        /*public override IEnumerable<string> GetFiles(string path) => (string[])Directory.EnumerateFiles(GetFullPath(path));
        public override IEnumerable<string> GetDirectories(string path) => Directory.GetDirectories(GetFullPath(path));
        public override void Delete(string path)
        {
            FileSafety.FileDelete(GetFullPath(path));
        }
        public override bool Exists(string path)
        {
            return File.Exists(GetFullPath(path));
        }
        public override void DeleteDirectory(string path)
        {
            path = GetFullPath(path);
            // handles the case where the directory doesn't exist, which will throw a DirectoryNotFoundException.
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }
        public override bool ExistsDirectory(string path) => Directory.Exists(GetUsablePathFor(path));
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
        public override void DeleteDatabase(string name) => Delete($@"{name}.db");*/

        public override void OpenInNativeExplorer()
        {
            //Not needed now.
            throw new NotImplementedException();
        }

        /*public override string GetFullPath(string path, bool createIfNotExisting = false)
        {
            throw new NotImplementedException();
        }

        public override Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
        {
            throw new NotImplementedException();
        }*/
    }
}
