using System;
using System.IO;
using SQLite;

namespace osu.Framework.OS
{
    public abstract class BasicStorage
    {
        public string BaseName { get; set; }
    
        public BasicStorage(string baseName)
        {
            BaseName = baseName;
        }

        public abstract Stream GetStream(string path, FileAccess mode = FileAccess.Read);
        public abstract SQLiteConnection GetDb(string name);
    }
}