// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;

namespace osu.Framework.Extensions.Vector2Extensions
{
    public static class Vector2Extensions
    {
        public static Vector2 ComponentClamp(this Vector2 vector, Vector2 min, Vector2 max)
        {
            return Vector2.ComponentMin(max, Vector2.ComponentMax(vector, min));
        }
    }
}
