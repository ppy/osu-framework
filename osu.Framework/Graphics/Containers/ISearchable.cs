using System;
using System.Collections.Generic;

namespace osu.Framework.Graphics.Containers
{
    public interface ISearchable
    {
        string[] Keywords { get; }
    }

    public interface ISearchableChildren
    {
        IEnumerable<Drawable> SearchableChildren { get; }

        Action AfterSearch { get; }
    }
}
