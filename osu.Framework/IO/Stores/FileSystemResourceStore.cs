﻿// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;

namespace osu.Framework.IO.Stores
{
    public class FileSystemResourceStore : ChangeableResourceStore<byte[]>, IDisposable
    {
        private FileSystemWatcher watcher;
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

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;

            watcher.Renamed -= watcherChanged;
            watcher.Changed -= watcherChanged;
            watcher.Created -= watcherChanged;

            watcher.Dispose();
        }

        #endregion

        private void watcherChanged(object sender, FileSystemEventArgs e)
        {
            TriggerOnChanged(e.FullPath.Replace(directory, string.Empty));
        }

        public override byte[] Get(string name)
        {
            return System.IO.File.ReadAllBytes(Path.Combine(directory, name));
        }
    }
}
