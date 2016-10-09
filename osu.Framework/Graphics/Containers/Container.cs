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
            get
            {
                if (Content != this)
                    return Content.Children;
                else
                    return children;
            }
            set
            {
                if (Content != this)
                    Content.Children = value;
                else
                {
                    if (!IsLoaded)
                        pendingChildren = value;
                    else
                    {
                        Clear();
                        AddInternal(value);
                    }
                }
            }
        }

        protected virtual IEnumerable<Drawable> InternalChildren
        {
            get { return children; }
            set
            {
                if (!IsLoaded)
                    pendingChildren = value;
                else
                {
                    Clear();
                    AddInternal(value);
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
        internal Vector2 ChildScale = Vector2.One;

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        internal virtual Vector2 ChildOffset => Vector2.Zero;

        /// <summary>
        /// The Size (coordinate space) revealed to Children.
        /// </summary>
        internal virtual Vector2 ChildSize => Size;

        /// <summary>
        /// Add a Drawable to Content's children list, recursing until Content == this.
        /// </summary>
        /// <param name="drawable">The drawable to be added.</param>
        public virtual void Add(Drawable drawable)
        {
            Debug.Assert(drawable != null, "null-Drawables may not be added to Containers.");
            Debug.Assert(Content != drawable, "Content may not be added to itself.");

            if (Content == this)
                AddInternal(drawable);
            else
                Content.Add(drawable);
        }

        /// <summary>
        /// Add a collection of Drawables to Content's children list, recursing until Content == this.
        /// </summary>
        /// <param name="drawable">The drawable to be added.</param>
        public void Add(IEnumerable<Drawable> collection)
        {
            foreach (Drawable d in collection)
                Add(d);
        }

        /// <summary>
        /// Add a Drawable to this container's Children list, disregarding the value of Content.
        /// </summary>
        /// <param name="drawable">The drawable to be added.</param>
        private void AddInternal(Drawable drawable)
        {
            Debug.Assert(drawable != null, "null-Drawables may not be added to Containers.");

            drawable.ChangeParent(this);
            children.Add(drawable);
        }

        /// <summary>
        /// Add a collection of Drawables to this container's Children list, disregarding the value of Content.
        /// </summary>
        /// <param name="drawable">The drawables to be added.</param>
        private void AddInternal(IEnumerable<Drawable> collection)
        {
            foreach (Drawable d in collection)
                AddInternal(d);
        }

        public virtual bool Remove(Drawable drawable, bool dispose = true)
        {
            if (drawable == null)
                return false;

            if (Content != this)
                return Content.Remove(drawable, dispose);

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
                Remove(p, dispose);
        }

        public virtual void Clear(bool dispose = true)
        {
            if (Content != null && Content != this)
            {
                Content.Clear(dispose);
                return;
            }

            foreach (Drawable t in children)
            {
                if (dispose)
                {
                    //cascade disposal
                    (t as Container)?.Clear();

                    t.Dispose();
                }
                t.Parent = null;
            }

            children.Clear();

            Invalidate(Invalidation.Position | Invalidation.SizeInParentSpace);
        }

        internal IEnumerable<Drawable> AliveChildren => children.AliveItems;

        protected virtual Container Content => this;

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
                AddInternal(pendingChildren);
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

        public override void Flush(bool propagateChildren = false)
        {
            base.Flush(propagateChildren);

            if (propagateChildren)
                foreach (var c in children) c.Flush(true);
        }

        public override Drawable DelayReset()
        {
            base.DelayReset();
            foreach (var c in children) c.DelayReset();

            return this;
        }
    }
}
