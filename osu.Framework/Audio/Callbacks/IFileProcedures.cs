// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using ManagedBass;

namespace osu.Framework.Audio.Callbacks
{
    /// <summary>
    /// Interface that closely matches the definition of the <see cref="FileProcedures" /> struct.
    /// </summary>
    public interface IFileProcedures
    {
        long Length(IntPtr user);
        void Close(IntPtr user);
        bool Seek(long offset, IntPtr user);
        int Read(IntPtr buffer, int length, IntPtr user);
    }
}
