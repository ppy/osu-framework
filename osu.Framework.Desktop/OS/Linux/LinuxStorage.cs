using System;
using System.IO;

namespace osu.Framework.Desktop.OS.Linux
{
    public class LinuxStorage : DesktopStorage
    {
        public LinuxStorage(string baseName) : base(baseName)
        {
        }
        
        protected override string BasePath
        {
            get
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                var paths = new[] {
                    Path.Combine(xdg ?? Path.Combine(home, ".local", "share"), BaseName),
                    Path.Combine(home, "." + BaseName)
                };
                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                        return path;
                }
                return paths[0];
            }
        }
    }
}