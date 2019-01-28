﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace osu.Framework.IO.Stores
{
    public class DllResourceStore : IResourceStore<byte[]>
    {
        private readonly Assembly assembly;
        private readonly string space;

        public DllResourceStore(string dllName)
        {
            assembly = Assembly.LoadFrom(Path.Combine(Path.GetDirectoryName(Assembly.GetCallingAssembly().Location), dllName));
            space = Path.GetFileNameWithoutExtension(dllName);
        }

        public byte[] Get(string name)
        {
            using (Stream input = GetStream(name))
            {
                if (input == null)
                    return null;

                byte[] buffer = new byte[input.Length];
                input.Read(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public virtual async Task<byte[]> GetAsync(string name)
        {
            using (Stream input = GetStream(name))
            {
                if (input == null)
                    return null;

                byte[] buffer = new byte[input.Length];
                await input.ReadAsync(buffer, 0, buffer.Length);
                return buffer;
            }
        }

        public Stream GetStream(string name)
        {
            var split = name.Split('/');
            for (int i = 0; i < split.Length - 1; i++)
                split[i] = split[i].Replace('-', '_');

            return assembly?.GetManifestResourceStream($@"{space}.{string.Join(".", split)}");
        }

        #region IDisposable Support

        private bool isDisposed;

        protected virtual void Dispose(bool disposing)
        {
            if (!isDisposed)
            {
                isDisposed = true;
            }
        }

        ~DllResourceStore()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}
