// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Extensions.Vector2Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2 Clamp(this Vector2 self, Vector2 lower, Vector2 upper)
        {
            return Vector2.ComponentMax(Vector2.ComponentMin(self, upper), lower);
        }
    }
}
