// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Android.Graphics;
using osu.Framework.Graphics.Primitives;

namespace osu.Framework.Android
{
    public static class AndroidExtensions
    {
        public static RectangleI ToRectangleI(this Rect rect) => new RectangleI(rect.Left, rect.Top, rect.Width(), rect.Height());
    }
}
