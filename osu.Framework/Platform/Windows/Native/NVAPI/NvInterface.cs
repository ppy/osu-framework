// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Framework.Platform.Windows.Native.NVAPI
{
    /// <summary>
    /// Marker attribute for mapping NVAPI function pointers to interface IDs. See <see cref="NvLibrary"/>
    /// </summary>
    public class NvInterfaceAttribute : Attribute
    {
        public uint Id { get; }

        public NvInterfaceAttribute(uint id)
        {
            Id = id;
        }
    }
}
