// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Framework.Graphics.Performance
{
    public interface IGlobalStatistic
    {
        string Group { get; }

        string Name { get; }

        IBindable<string> DisplayValue { get; }
    }
}
