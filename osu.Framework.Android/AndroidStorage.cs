using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using osu.Framework.Platform;

namespace osu.Framework.Android
{
    class AndroidStorage : DesktopStorage
    {
        public AndroidStorage(string baseName, GameHost host)
            : base(baseName, host)
        {
            BasePath = LocateBasePath();
        }

        //Dopilnuj, aby zamieniać ścieżki względne na bezwzględne

        protected override string LocateBasePath()
        {
            return (string)Application.Context.GetExternalFilesDir("");
        }

        /*public override string[] GetFiles(string path) => (string[])Directory.EnumerateFiles(GetUsablePathFor(path));
        public override string[] GetDirectories(string path) => Directory.GetDirectories(GetUsablePathFor(path));
        public override void Delete(string path)
        {
            FileSafety.FileDelete(GetUsablePathFor(path));
        }
        public override bool Exists(string path)
        {
            return File.Exists(GetUsablePathFor(path));
        }
        public override void DeleteDirectory(string path)
        {
            path = GetUsablePathFor(path);
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
    }
}
