// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;

namespace osu.Framework.Platform
{
    public interface ISystemFileSelector : IDisposable
    {
        /// <summary>
        /// Triggered when the user has selected a file.
        /// </summary>
        event Action<FileInfo> Selected;

        /// <summary>
        /// Presents the system file selector.
        /// </summary>
        void Present();
    }
}
