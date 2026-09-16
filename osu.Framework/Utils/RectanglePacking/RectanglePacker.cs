// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Utils.RectanglePacking
{
    public abstract class RectanglePacker : IRectanglePacker
    {
        public Vector2I BinSize { get; protected set; }

        protected RectanglePacker(Vector2I binSize)
        {
            BinSize = binSize;
            Reset();
        }

        public abstract Vector2I? TryAdd(int width, int height);

        public abstract void Reset();
    }
}