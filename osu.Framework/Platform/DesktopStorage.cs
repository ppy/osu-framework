// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.IO.File;

namespace osu.Framework.Platform
{
    public class DesktopStorage : Storage
    {
        private readonly GameHost host;

        public DesktopStorage(string baseName, GameHost host)
            : base(baseName)
        {
            this.host = host;
        }

        protected override string LocateBasePath() => @"./"; //use current directory by default

        public override bool Exists(string path) => File.Exists(GetFullPath(path));

        public override bool ExistsDirectory(string path) => Directory.Exists(GetFullPath(path));

        public override void DeleteDirectory(string path)
        {
            path = GetFullPath(path);

            // handles the case where the directory doesn't exist, which will throw a DirectoryNotFoundException.
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public override void Delete(string path) => FileSafety.FileDelete(GetFullPath(path));

        public override IEnumerable<string> GetDirectories(string path) => getRelativePaths(Directory.GetDirectories(GetFullPath(path)));

        public override IEnumerable<string> GetFiles(string path) => getRelativePaths(Directory.GetFiles(GetFullPath(path)));

        private IEnumerable<string> getRelativePaths(IEnumerable<string> paths)
        {
            string basePath = Path.GetFullPath(GetFullPath(string.Empty));
            return paths.Select(Path.GetFullPath).Select(path =>
            {
                if (!path.StartsWith(basePath)) throw new ArgumentException($"\"{path}\" does not start with \"{basePath}\" and is probably malformed");
                return path.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar);
            });
        }

        public override string GetFullPath(string path, bool createIfNotExisting = false)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            var basePath = Path.GetFullPath(Path.Combine(BasePath, BaseName, SubDirectory));
            var resolvedPath = Path.GetFullPath(Path.Combine(basePath, path));

            if (!resolvedPath.StartsWith(basePath)) throw new ArgumentException($"\"{resolvedPath}\" traverses outside of \"{basePath}\" and is probably malformed");

            if (createIfNotExisting) Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath));
            return resolvedPath;
        }

        public override void OpenInNativeExplorer() => host.OpenFileExternally(GetFullPath(string.Empty));

        public override Stream GetStream(string path, FileAccess access = FileAccess.Read, FileMode mode = FileMode.OpenOrCreate)
        {
            path = GetFullPath(path, access != FileAccess.Read);

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
            return string.Concat("Data Source=", GetFullPath($@"{name}.db", true));
        }

        public override void DeleteDatabase(string name) => Delete($@"{name}.db");
    }
}
