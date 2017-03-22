// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface ISearchable
    {
        string[] Keywords { get; }

        bool LastMatch { get; set; }
    }

    public interface ISearchableChildren : ISearchable
    {
        IEnumerable<Drawable> SearchableChildren { get; }

        Action AfterSearch { get; }
    }
}
