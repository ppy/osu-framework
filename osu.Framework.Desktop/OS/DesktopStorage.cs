using System;
using SQLite;
using System.IO;
using osu.Framework.OS;

namespace osu.Framework.Desktop.OS
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
        
        public override SQLiteConnection GetDb(string name)
        {
            Directory.CreateDirectory(BasePath);
            return new SQLiteConnection(Path.Combine(BasePath, $@"{name}.db"));
        }
    }
}