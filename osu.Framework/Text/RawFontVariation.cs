// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Text
{
    /// <summary>
    /// Represents the raw configuration of an OpenType variable font passed to
    /// FreeType.
    /// </summary>
    public class RawFontVariation
    {
        /// <summary>
        /// The named instance to use.
        /// </summary>
        /// <remarks>
        /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
        /// only <see cref="Axes"/> is used.
        /// </remarks>
        public uint NamedInstance { get; init; }

        /// <summary>
        /// The configuration of the variable font.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Numbers are in 16.16 fixed point format.
        /// </para>
        /// <para>
        /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
        /// only <see cref="Axes"/> is used.
        /// </para>
        /// </remarks>
        public ReadOnlyMemory<CLong> Axes { get; init; }
    }
}
