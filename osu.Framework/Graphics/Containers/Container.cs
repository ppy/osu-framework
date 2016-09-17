//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics;
using osu.Framework.Lists;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public class Container : Drawable
    {
        public bool Masking;

        protected override DrawNode BaseDrawNode => new ContainerDrawNode(DrawInfo, Masking ? ScreenSpaceDrawQuad.BoundingRectangle : (Rectangle?)null);

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

        public ReadOnlyList<Drawable> Children => base.Children;

        public IEnumerable<Drawable> Kids //todo: think of a better name
        {
            set
            {
                if (!IsLoaded)
                    pendingChildren = value;
                else
                {
                    Clear();
                    Add(value);
                }
            }
        }

        private IEnumerable<Drawable> pendingChildren;

        public override void Load()
        {
            base.Load();

            if (pendingChildren != null)
            {
                Add(pendingChildren);
                pendingChildren = null;
            }
        }
    }
}
