using System;
using System.IO;

namespace osu.Framework.Desktop.Platform.Windows
{
    public class WindowsStorage : DesktopStorage
    {
        public WindowsStorage(string baseName) : base(baseName)
        {
        }

        protected override string BasePath
        {
            get
            {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                return Path.Combine(appdata, BaseName);
            }
        }    }
}