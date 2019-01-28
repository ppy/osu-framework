﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class FileSystemResourceStore : ChangeableResourceStore<byte[]>
    {
        private readonly FileSystemWatcher watcher;
        private readonly string directory;

        private bool isDisposed;

        public FileSystemResourceStore(string directory)
        {
            this.directory = directory;

            watcher = new FileSystemWatcher(directory)
            {
                EnableRaisingEvents = true
            };
            watcher.Renamed += watcherChanged;
            watcher.Changed += watcherChanged;
            watcher.Created += watcherChanged;
        }

        #region Disposal

        ~FileSystemResourceStore()
        {
            Dispose(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            watcher.Dispose();
        }

        #endregion

        private void watcherChanged(object sender, FileSystemEventArgs e)
        {
            TriggerOnChanged(e.FullPath.Replace(directory, string.Empty));
        }

        public override async Task<byte[]> GetAsync(string name)
        {
            byte[] result;

            using (FileStream stream = System.IO.File.OpenRead(Path.Combine(directory, name)))
            {
                result = new byte[stream.Length];
                await stream.ReadAsync(result, 0, (int)stream.Length);
            }

            return result;
        }
    }
}
