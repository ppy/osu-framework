// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Timing;
using System;
using System.Diagnostics;
using OpenTK;

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

        private LifetimeList<Drawable> children;
        private IEnumerable<Drawable> pendingChildren;

        public virtual IEnumerable<Drawable> Children
        {
            get { return children; }
            set
            {
                if (AddTarget != null && AddTarget != this)
                {
                    AddTarget.Children = value;
                }
                else if (!IsLoaded)
                    pendingChildren = value;
                else
                {
                    Clear();
                    Add(value);
                }
            }
        }

        public Container()
        {
            children = new LifetimeList<Drawable>(DepthComparer);
        }

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        internal Vector2 ChildrenScale = Vector2.One;

        public virtual Drawable Add(Drawable drawable)
        {
            if (drawable == null)
                return null;

            if (AddTarget == this || AddTarget == drawable)
                return AddTopLevel(drawable);

            return AddTarget.Add(drawable);
        }

        public void Add(IEnumerable<Drawable> collection)
        {
            foreach (Drawable d in collection)
                Add(d);
        }

        public virtual bool Remove(Drawable drawable, bool dispose = true)
        {
            if (drawable == null)
                return false;

            if (AddTarget != this)
                return AddTarget.Remove(drawable, dispose);

            bool result = children.Remove(drawable);
            drawable.Parent = null;

            if (dispose && drawable.IsDisposable)
                drawable.Dispose();
            else
                drawable.Invalidate();

            return result;
        }

        public int RemoveAll(Predicate<Drawable> match, bool dispose = true)
        {
            List<Drawable> toRemove = children.FindAll(match);
            for (int i = 0; i < toRemove.Count; i++)
                Remove(toRemove[i], dispose);

            return toRemove.Count;
        }

        public void Remove(IEnumerable<Drawable> range, bool dispose = true)
        {
            if (range == null)
                return;

            foreach (Drawable p in range)
            {
                if (p.IsDisposable)
                    p.Dispose();
                Remove(p);
            }
        }

        public virtual void Clear(bool dispose = true)
        {
            if (AddTarget != null && AddTarget != this)
            {
                AddTarget.Clear(dispose);
                return;
            }

            foreach (Drawable t in children)
            {
                if (dispose)
                    t.Dispose();
                t.Parent = null;
            }

            children.Clear();

            Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
        }

        protected Drawable AddTopLevel(Drawable drawable)
        {
            drawable.ChangeParent(this);
            children.Add(drawable);
            return drawable;
        }

        internal IEnumerable<Drawable> AliveChildren => children.AliveItems;

        protected virtual Container AddTarget => this;

        internal override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            UpdateChildrenLife();

            foreach (Drawable child in children.AliveItems)
                child.UpdateSubTree();

            UpdateLayout();
            return true;
        }

        public override void Load()
        {
            if (pendingChildren != null)
            {
                Add(pendingChildren);
                pendingChildren = null;
            }

            base.Load();
        }

        public override bool Invalidate(Invalidation invalidation = Invalidation.All, Drawable source = null, bool shallPropagate = true)
        {
            if (!base.Invalidate(invalidation, source, shallPropagate))
                return false;

            if (shallPropagate)
            {
                foreach (var c in children)
                {
                    Debug.Assert(c != source);

                    Invalidation childInvalidation = invalidation;
                    if (c.RelativeSizeAxes == Axes.None)
                        childInvalidation = childInvalidation & ~Invalidation.SizeInParentSpace;

                    c.Invalidate(childInvalidation, this);
                }
            }

            return true;
        }

        /// <summary>
        /// Updates the life status of children according to their IsAlive property.
        /// </summary>
        /// <returns>True iff the life status of at least one child changed.</returns>
        protected virtual bool UpdateChildrenLife()
        {
            return children.Update();
        }

        internal override void ChangeRoot(Game root)
        {
            base.ChangeRoot(root);

            foreach (Drawable c in children)
                //use Game here to make sure we respect any decisions base.ChangeRoot made.
                c.ChangeRoot(Game);
        }

        /// <summary>
        /// Perform any layout changes just before autosize is calculated.		
        /// </summary>		
        protected virtual void UpdateLayout()
        {
        }

        internal override DrawNode GenerateDrawNodeSubtree(DrawNode node = null)
        {
            ContainerDrawNode cNode = base.GenerateDrawNodeSubtree(node) as ContainerDrawNode;

            if (children.AliveItems.Count > 0)
            {
                if (cNode.Children != null)
                {
                    var current = children.AliveItems;
                    var target = cNode.Children;

                    int j = 0;
                    for (int i = 0; i < current.Count; i++)
                    {
                        if (!current[i].IsVisible) continue;

                        //todo: make this more efficient.
                        if (Game?.ScreenSpaceDrawQuad.FastIntersects(current[i].ScreenSpaceDrawQuad) == false)
                            continue;

                        if (j < target.Count && target[j].Drawable == current[i])
                        {
                            current[i].GenerateDrawNodeSubtree(target[j]);
                        }
                        else
                        {
                            if (j < target.Count)
                                target.RemoveAt(j);
                            target.Insert(j, current[i].GenerateDrawNodeSubtree());
                        }

                        j++;
                    }

                    if (j < target.Count)
                        target.RemoveRange(j, target.Count - j);

                }
                else
                {
                    cNode.Children = new List<DrawNode>(children.AliveItems.Count);

                    foreach (Drawable child in children.AliveItems)
                    {
                        //if (Game?.ScreenSpaceDrawQuad.Intersects(child.ScreenSpaceDrawQuad) == false)
                        //    continue;

                        if (!child.IsVisible)
                            continue;

                        cNode.Children.Add(child.GenerateDrawNodeSubtree());
                    }
                }
            }
            else
            {
                cNode.Children?.Clear();
            }

            return cNode;
        }

        public override Drawable Delay(double duration, bool propagateChildren = false)
        {
            if (duration == 0) return this;

            base.Delay(duration, propagateChildren);

            if (propagateChildren)
                foreach (var c in children) c.Delay(duration, true);
            return this;
        }

        public override Drawable DelayReset()
        {
            base.DelayReset();
            foreach (var c in children) c.DelayReset();

            return this;
        }
    }
}
