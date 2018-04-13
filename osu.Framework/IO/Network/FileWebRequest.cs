// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.IO;
using osu.Framework.IO.File;

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

        protected override void Complete(Exception e = null)
        {
            ResponseStream?.Close();
            if (e != null) FileSafety.FileDelete(Filename);
            base.Complete(e);
        }
    }
}
