// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osuTK;

namespace osu.Framework.Input.Handlers.Tablet
{
    /// <summary>
    /// A class that carries the information we care about from the tablet provider.
    /// Can be considered for removal when we no longer require dual targeting against netstandard.
    /// </summary>
    public class TabletInfo
    {
        /// <summary>
        /// The name of this tablet.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The size (in millimetres) of the connected tablet's full area.
        /// </summary>
        public Vector2 Size { get; }

        public TabletInfo(string name, Vector2 size)
        {
            Size = size;
            Name = name;
        }
    }
}
