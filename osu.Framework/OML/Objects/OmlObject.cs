// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using JetBrains.Annotations;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.OML.Objects
{
    public class OmlObject : OmlObject<Drawable>
    {
    }

    [UsedImplicitly]
    [MeansImplicitUse]
    public class OmlObject<T> : Container<T>
        where T : Drawable
    {
        public bool IsDefaultObject = true;

        public virtual Bindable<string> BindableValue { get; set; } // a Value is always a string and can't be something other than a string.
    }
}
