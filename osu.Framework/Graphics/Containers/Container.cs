//Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
//Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Drawing;
using OpenTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public class Container : Drawable
    {
        public bool Masking;

        protected override DrawNode BaseDrawNode => new ContainerDrawNode(DrawInfo, Masking ? ScreenSpaceDrawQuad.BoundingRectangle : (Rectangle?)null);

        public new IEnumerable<Drawable> Children
        {
            get { return base.Children; }
            set
            {
                if (AddTarget == this)
                    base.Children = value;
                else
                    AddTarget.Children = value;
            }
        }

        protected virtual Container AddTarget => this;

        public new virtual Drawable Add(Drawable drawable)
        {
            if (AddTarget == this || AddTarget == drawable)
                return AddTopLevel(drawable);

            return AddTarget.Add(drawable);
        }

        protected Drawable AddTopLevel(Drawable drawable)
        {
            return base.Add(drawable);
        }

        public new void Add(IEnumerable<Drawable> drawables)
        {
            foreach (Drawable d in drawables)
                Add(d);
        }

        public new virtual bool Remove(Drawable drawable, bool dispose = true)
        {
            if (AddTarget == this)
                return base.Remove(drawable, dispose);

            return AddTarget.Remove(drawable, dispose);
        }

        public new virtual void Remove(IEnumerable<Drawable> drawables, bool dispose = true)
        {
            foreach (Drawable d in drawables)
                Remove(d, dispose);
        }

        public virtual int RemoveAll(Predicate<Drawable> match)
        {
            if (AddTarget == this)
                return base.RemoveAll(match);

            return AddTarget.RemoveAll(match);
        }

        public new virtual void Clear(bool dispose = true)
        {
            if (AddTarget == this)
                base.Clear(dispose);
            else
                AddTarget.Clear(dispose);
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
