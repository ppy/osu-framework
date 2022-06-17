// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.IO;
using System.Threading.Tasks;

namespace osu.Framework.IO.Network
{
    /// <summary>
    /// Downloads a file from the internet to a specified location
    /// </summary>
    public class FileWebRequest : WebRequest
    {
        public string Filename;

        protected override string Accept => "application/octet-stream";

        protected override Stream CreateOutputStream()
        {
            string path = Path.GetDirectoryName(Filename);
            if (!string.IsNullOrEmpty(path)) Directory.CreateDirectory(path);

            return new FileStream(Filename, FileMode.Create, FileAccess.Write, FileShare.Write, 32768);
        }

        public FileWebRequest(string filename, string url)
            : base(url)
        {
            Timeout *= 2;
            Filename = filename;
        }

        protected override Task Complete(Exception e = null)
        {
            ResponseStream?.Close();

            if (e != null)
            {
                try
                {
                    File.Delete(Filename);
                }
                catch (Exception eOther)
                {
                    e = new AggregateException(e, eOther);
                }
            }

            return base.Complete(e);
        }
    }
}
