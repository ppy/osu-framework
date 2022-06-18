// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using ManagedBass;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Interface that closely matches the definition of the <see cref="FileProcedures"/> struct.
    /// </summary>
    public interface IFileProcedures
    {
        long Length(IntPtr user);
        void Close(IntPtr user);
        bool Seek(long offset, IntPtr user);
        int Read(IntPtr buffer, int length, IntPtr user);
    }
}
