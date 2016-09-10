//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public class Container : Drawable
    {
        public new virtual Drawable Add(Drawable drawable)
        {
            return base.Add(drawable);
        }

        public new void Add(IEnumerable<Drawable> drawables)
        {
            base.Add(drawables);
        }

        public new virtual bool Remove(Drawable drawable, bool dispose = true)
        {
            return base.Remove(drawable, dispose);
        }

        public new void Remove(IEnumerable<Drawable> drawables, bool dispose = true)
        {
            base.Remove(drawables);
        }

        public int RemoveAll(Predicate<Drawable> match)
        {
            return base.RemoveAll(match);
        }

        public new virtual void Clear(bool dispose = true)
        {
            base.Clear(dispose);
        }

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        public new Vector2 ContentScale
        {
            get { return base.ContentScale; }
            set { base.ContentScale = value; }
        }
    }
}
