// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System.Collections.Generic;
using System.Drawing;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public class Container : Drawable
    {
        public bool Masking;

        protected override DrawNode CreateDrawNode() => new ContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            ContainerDrawNode n = node as ContainerDrawNode;

            n.MaskingRect = Masking ? ScreenSpaceDrawQuad.AABB : (Rectangle?)null;

            base.ApplyDrawNode(node);
        }

        public override bool HandleInput => true;

        public override IEnumerable<Drawable> Children
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

        public override Drawable Add(Drawable drawable)
        {
            if (AddTarget == this || AddTarget == drawable)
                return AddTopLevel(drawable);

            return AddTarget.Add(drawable);
        }

        protected Drawable AddTopLevel(Drawable drawable)
        {
            return base.Add(drawable);
        }

        public override bool Remove(Drawable drawable, bool dispose = true)
        {
            if (AddTarget == this)
                return base.Remove(drawable, dispose);

            return AddTarget.Remove(drawable, dispose);
        }

        public override void Clear(bool dispose = true)
        {
            if (AddTarget == this)
                base.Clear(dispose);
            else
                AddTarget.Clear(dispose);
        }
    }
}
