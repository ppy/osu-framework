// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace osu.Framework.Text
{
    /// <summary>
    /// Represents the configuration of an OpenType variable font.
    /// </summary>
    public class FontVariation
    {
        /// <summary>
        /// The named instance to use.
        /// </summary>
        /// <remarks>
        /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
        /// only <see cref="Axes"/> is used.
        /// </remarks>
        public string? NamedInstance { get; init; }

        /// <summary>
        /// The configuration of the variable font.
        /// </summary>
        /// <remarks>
        /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
        /// only <see cref="Axes"/> is used.
        /// </remarks>
        public IEnumerable<KeyValuePair<string, double>>? Axes { get; init; }

        /// <summary>
        /// Generate a suitable font name suffix for this configuration.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If both <see cref="NamedInstance"/> and <see cref="Axes"/> are set,
        /// only <see cref="Axes"/> is used.
        /// </para>
        /// <para>
        /// The name generation follows Adobe TechNote #5902, 'Generating
        /// PostScript Names for Fonts Using OpenType Font Variations'.
        /// </para>
        /// <seealso href="https://download.macromedia.com/pub/developer/opentype/tech-notes/5902.AdobePSNameGeneration.html"/>
        /// </remarks>
        public string GenerateInstanceName(string baseName)
        {
            if (Axes is not null)
            {
                var instanceName = new StringBuilder(baseName);
                var hashedAxes = new List<HashedAxisParameter>();

                foreach (var (axis, value) in Axes)
                {
                    // add parameter for hashing
                    var parameter = new HashedAxisParameter();

                    unsafe
                    {
                        NativeMemory.Fill(parameter.Axis, 4, 0x20);
                        Encoding.UTF8.GetBytes(axis, new Span<byte>(parameter.Axis, 4));
                    }

                    parameter.Value = (int)Math.Round(value * 65536.0);
                    hashedAxes.Add(parameter);

                    // compute ASCII representation of parameter
                    double effectiveValue = parameter.Value / 65536.0;

                    instanceName.Append($@"_{effectiveValue:0.#####}{axis}");
                }

                if (instanceName.Length > 127)
                {
                    // The last resort string is constructed from a SHA-256 hash
                    // of the hashed parameters if the string form of the parameters
                    // gets too long.
                    ReadOnlySpan<byte> hashedData = MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(hashedAxes));
                    return $@"{baseName}-{Convert.ToHexString(SHA256.HashData(hashedData))}";
                }

                return instanceName.ToString();
            }
            else if (NamedInstance is not null)
            {
                return NamedInstance;
            }
            else
            {
                return baseName;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private unsafe struct HashedAxisParameter
        {
            public fixed byte Axis[4];
            public int Value;
        }
    }
}
