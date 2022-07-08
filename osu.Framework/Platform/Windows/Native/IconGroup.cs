// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.IO;
using System.Runtime.InteropServices;
using System;
using System.Linq;
using JetBrains.Annotations;

namespace osu.Framework.Platform.Windows.Native
{
    internal class IconGroup
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct IconDirEntry
        {
            internal byte Width;
            internal byte Height;
            internal byte ColourCount;
            internal byte Reserved;
            internal ushort Planes;
            internal ushort BitCount;
            internal uint BytesInResource;
            internal uint ImageOffset;

            // larger icons are defined as 0x0 and point to PNG data
            internal readonly bool HasRawData => Width == 0 && Height == 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct IconDir
        {
            internal ushort Reserved;
            internal ushort Type;
            internal ushort Count;
            internal IconDirEntry[] Entries;
        }

        private const uint lr_defaultcolor = 0x00000000;

        private IconDir iconDir;
        private byte[] data;

        public static bool TryParse(byte[] data, out IconGroup iconGroup)
        {
            try
            {
                iconGroup = new IconGroup(data);
                return true;
            }
            catch (Exception)
            {
            }

            iconGroup = null;
            return false;
        }

        public IconGroup(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("Missing icon stream.", nameof(stream));

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                loadMemoryStream(ms);
            }
        }

        public IconGroup(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentException("Missing icon data.", nameof(data));

            using (var ms = new MemoryStream(data))
                loadMemoryStream(ms);
        }

        private void loadMemoryStream(MemoryStream stream)
        {
            data = stream.ToArray();
            stream.Position = 0;

            var reader = new BinaryReader(stream);
            iconDir.Reserved = reader.ReadUInt16();
            if (iconDir.Reserved != 0)
                throw new ArgumentException("Invalid icon format.", nameof(stream));

            iconDir.Type = reader.ReadUInt16();
            if (iconDir.Type != 1)
                throw new ArgumentException("Invalid icon format.", nameof(stream));

            iconDir.Count = reader.ReadUInt16();
            iconDir.Entries = new IconDirEntry[iconDir.Count];

            for (int i = 0; i < iconDir.Count; i++)
            {
                iconDir.Entries[i] = new IconDirEntry
                {
                    Width = reader.ReadByte(),
                    Height = reader.ReadByte(),
                    ColourCount = reader.ReadByte(),
                    Reserved = reader.ReadByte(),
                    Planes = reader.ReadUInt16(),
                    BitCount = reader.ReadUInt16(),
                    BytesInResource = reader.ReadUInt32(),
                    ImageOffset = reader.ReadUInt32()
                };
            }
        }

        /// <summary>
        /// Finds the closest icon entry index that is less than or equal to the requested size and bit depth.
        /// Icon size takes priority over bit depth since a scaled 32-bit icon will look better than a correctly-sized 8-bit icon.
        /// </summary>
        /// <param name="width">The maximum desired width in pixels.</param>
        /// <param name="height">The maximum desired height in pixels.</param>
        /// <param name="bpp">The maximum desired bit depth.</param>
        /// <param name="requireRawData">If true, only icon entries that provide raw PNG data will be considered.</param>
        /// <returns>The index of the icon in the icon directory, or -1 if a valid icon could not be found.</returns>
        private int findClosestEntry(int width, int height, int bpp, bool requireRawData) =>
            Enumerable.Range(0, iconDir.Count)
                      .Where(i => iconDir.Entries[i].Width <= width && iconDir.Entries[i].Height <= height && iconDir.Entries[i].BitCount <= bpp)
                      .Where(i => iconDir.Entries[i].HasRawData || !requireRawData)
                      .OrderByDescending(i => iconDir.Entries[i].Width)
                      .ThenByDescending(i => iconDir.Entries[i].Height)
                      .ThenByDescending(i => iconDir.Entries[i].BitCount)
                      .DefaultIfEmpty(-1).First();

        /// <summary>
        /// Attempts to create a Windows-specific icon matching the requested dimensions as closely as possible.
        /// Will return null if a matching size could not be found.
        /// </summary>
        /// <param name="width">The maximum desired width in pixels.</param>
        /// <param name="height">The maximum desired height in pixels.</param>
        /// <param name="bpp">The maximum desired bit count. Defaults to 32 bit.</param>
        /// <returns>An <see cref="Icon"/> instance, or null if a valid size could not be found.</returns>
        /// <exception cref="InvalidOperationException">If the native icon handle could not be created.</exception>
        [CanBeNull]
        public Icon CreateIcon(int width, int height, int bpp = 32)
        {
            int closest = findClosestEntry(width, height, bpp, false);
            if (closest < 0)
                return null;

            var entry = iconDir.Entries[closest];
            IntPtr hIcon = IntPtr.Zero;
            var span = new ReadOnlySpan<byte>(data, (int)entry.ImageOffset, (int)entry.BytesInResource);

            if (!entry.HasRawData)
                hIcon = CreateIconFromResourceEx(span.ToArray(), entry.BytesInResource, true, 0x00030000, width, height, lr_defaultcolor);

            if (hIcon == IntPtr.Zero)
                throw new InvalidOperationException("Couldn't create native icon handle.");

            return new Icon(hIcon, width, height);
        }

        /// <summary>
        /// Attempts to load the raw PNG data from a supported icon, matching the requested dimensions as closely as possible.
        /// Not all icons in a .ico file are stored as raw PNG data. Will return null if a matching raw PNG could not be found.
        /// </summary>
        /// <param name="width">The maximum desired width in pixels.</param>
        /// <param name="height">The maximum desired height in pixels.</param>
        /// <param name="bpp">The maximum desired bit count. Defaults to 32 bit.</param>
        /// <returns>A byte array of raw PNG data, or null if a valid size could not be found.</returns>
        [CanBeNull]
        public byte[] LoadRawIcon(int width, int height, int bpp = 32)
        {
            int closest = findClosestEntry(width, height, bpp, true);
            if (closest < 0)
                return null;

            var entry = iconDir.Entries[closest];
            var span = new ReadOnlySpan<byte>(data, (int)entry.ImageOffset, (int)entry.BytesInResource);

            return span.ToArray();
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconFromResourceEx(byte[] pbIconBits, uint cbIconBits, bool fIcon, uint dwVersion, int cxDesired, int cyDesired, uint uFlags);
    }
}
