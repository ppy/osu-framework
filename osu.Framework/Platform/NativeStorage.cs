// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JetBrains.Annotations;

namespace osu.Framework.Platform
{
    public class NativeStorage : Storage
    {
        private readonly GameHost host;

        public NativeStorage(string path, GameHost host = null)
            : base(path)
        {
            this.host = host;
        }

        public override bool Exists(string path) => File.Exists(GetFullPath(path));

        public override bool ExistsDirectory(string path) => Directory.Exists(GetFullPath(path));

        public override void DeleteDirectory(string path)
        {
            path = GetFullPath(path);

            // handles the case where the directory doesn't exist, which will throw a DirectoryNotFoundException.
            if (Directory.Exists(path))
                Directory.Delete(path, true);
        }

        public override void Delete(string path)
        {
            path = GetFullPath(path);

            if (File.Exists(path))
                File.Delete(path);
        }

        public override void Move(string from, string to) => File.Move(GetFullPath(from), GetFullPath(to));

        public override IEnumerable<string> GetDirectories(string path) => getRelativePaths(Directory.GetDirectories(GetFullPath(path)));

        public override IEnumerable<string> GetFiles(string path, string pattern = "*") => getRelativePaths(Directory.GetFiles(GetFullPath(path), pattern));

        private IEnumerable<string> getRelativePaths(IEnumerable<string> paths)
        {
            string basePath = Path.GetFullPath(GetFullPath(string.Empty));
            return paths.Select(Path.GetFullPath).Select(path =>
            {
                if (!path.StartsWith(basePath, StringComparison.Ordinal)) throw new ArgumentException($"\"{path}\" does not start with \"{basePath}\" and is probably malformed");

                return path.AsSpan(basePath.Length).TrimStart(Path.DirectorySeparatorChar).ToString();
            });
        }

        public override string GetFullPath(string path, bool createIfNotExisting = false)
        {
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            string basePath = Path.GetFullPath(BasePath).TrimEnd(Path.DirectorySeparatorChar);
            string resolvedPath = Path.GetFullPath(Path.Combine(basePath, path));

            if (!resolvedPath.StartsWith(basePath, StringComparison.Ordinal)) throw new ArgumentException($"\"{resolvedPath}\" traverses outside of \"{basePath}\" and is probably malformed");

            if (createIfNotExisting) Directory.CreateDirectory(Path.GetDirectoryName(resolvedPath));
            return resolvedPath;
        }

        public override bool OpenFileExternally(string filename) =>
            host?.OpenFileExternally(GetFullPath(filename)) == true;

        public override bool PresentFileExternally(string filename) =>
            host?.PresentFileExternally(GetFullPath(filename)) == true;

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
                    return new FlushingStream(path, mode, access);
            }
        }

        public override Storage GetStorageForDirectory([NotNull] string path)
        {
            if (path == null) throw new ArgumentNullException(nameof(path));

            if (path.Length > 0 && !path.EndsWith(Path.DirectorySeparatorChar))
                path += Path.DirectorySeparatorChar;

            // create non-existing path.
            string fullPath = GetFullPath(path, true);

            return (Storage)Activator.CreateInstance(GetType(), fullPath, host);
        }
    }
}
