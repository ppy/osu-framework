// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Runtime.InteropServices;
using System;

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
            internal bool HasRawData => Width == 0 && Height == 0;
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

        private readonly IconDir iconDir;
        private readonly byte[] data;

        public IconGroup(Stream stream)
        {
            if (stream == null || stream.Length == 0)
                throw new ArgumentException("Invalid icon stream.", nameof(stream));

            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                data = ms.GetBuffer();
                ms.Position = 0;

                var reader = new BinaryReader(ms);
                iconDir.Reserved = reader.ReadUInt16();
                if (iconDir.Reserved != 0)
                    throw new ArgumentException("Invalid icon stream.", nameof(stream));

                iconDir.Type = reader.ReadUInt16();
                if (iconDir.Type != 1)
                    throw new ArgumentException("Invalid icon stream.", nameof(stream));

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
        }

        private int findClosestEntry(int width, int height)
        {
            int requested = Math.Min(width, height);
            int closest = -1;
            for (int i = 0; i < iconDir.Count; i++)
            {
                var entry = iconDir.Entries[i];
                if (entry.Width == width && entry.Height == height)
                    return i;
                if (entry.Width > requested || entry.Height > requested)
                    continue;
                if (closest < 0 || entry.Width > iconDir.Entries[closest].Width || entry.Height > iconDir.Entries[closest].Height)
                    closest = i;
            }
            return closest;
        }

        public Icon CreateIcon(int width, int height)
        {
            int closest = findClosestEntry(width, height);
            if (closest < 0)
                throw new InvalidOperationException($"Couldn't find icon to match width {width} and height {height}.");

            var entry = iconDir.Entries[closest];
            IntPtr hIcon = IntPtr.Zero;
            var span = new ReadOnlySpan<byte>(data, (int)entry.ImageOffset, (int)entry.BytesInResource);

            if (!entry.HasRawData)
                hIcon = CreateIconFromResourceEx(span.ToArray(), entry.BytesInResource, true, 0x00030000, width, height, lr_defaultcolor);

            if (hIcon == IntPtr.Zero)
                throw new InvalidOperationException("Couldn't create native icon handle.");
            return new Icon(hIcon, width, height);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreateIconFromResourceEx(byte[] pbIconBits, uint cbIconBits, bool fIcon, uint dwVersion, int cxDesired, int cyDesired, uint uFlags);
    }
}
