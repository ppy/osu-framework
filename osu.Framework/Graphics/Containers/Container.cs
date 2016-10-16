// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System.Drawing;
using osu.Framework.Timing;
using System;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.OpenGL;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public class Container : Drawable
    {
        public bool Masking = false;

        private float cornerRadius = 0.0f;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines how large of a radius is masked away around the corners.
        /// </summary>
        public virtual float CornerRadius { get { return cornerRadius; } set { cornerRadius = value; } }

        protected override DrawNode CreateDrawNode() => new ContainerDrawNode();

        protected override void ApplyDrawNode(DrawNode node)
        {
            ContainerDrawNode n = node as ContainerDrawNode;

            n.MaskingInfo = !Masking ? (MaskingInfo?)null : new MaskingInfo
            {
                ScreenSpaceAABB = ScreenSpaceDrawQuad.AABB,
                MaskingRect = DrawQuad.AABBf,
                ToMaskingSpace = DrawInfo.MatrixInverse,
                CornerRadius = this.CornerRadius,
            };

            base.ApplyDrawNode(node);
        }

        public override bool HandleInput => true;

        private LifetimeList<Drawable> children;

        //todo: reference only used for screen bounds checking. we can probably remove this somehow.
        private BaseGame game;

        private List<Drawable> pendingChildrenInternal;
        private List<Drawable> pendingChildren => pendingChildrenInternal == null ? (pendingChildrenInternal = new List<Drawable>()) : pendingChildrenInternal;

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
                    InternalChildren = value;
            }
        }

        internal virtual IEnumerable<Drawable> InternalChildren
        {
            get { return IsLoaded ? children : pendingChildren; }

            set
            {
                if (!IsLoaded)
                {
                    Debug.Assert(pendingChildren.Count == 0, "Can not overwrite existing pending children.");
                    Clear();
                    pendingChildren.AddRange(value);
                }
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
            children.LoadRequested += loadChild;
        }

        private MarginPadding padding;
        public MarginPadding Padding
        {
            get { return padding; }
            set
            {
                if (padding.Equals(value)) return;

                padding = value;
    
                foreach (Drawable c in children)
                    c.Invalidate(Invalidation.Geometry, this);
            }
        }

        private MarginPadding margin;
        public MarginPadding Margin
        {
            get { return margin; }
            set
            {
                if (margin.Equals(value)) return;

                margin = value;

                Invalidate(Invalidation.Geometry);
            }
        }

        public override Vector2 Size => base.Size + new Vector2(Margin.TotalHorizontal, Margin.TotalVertical);

        /// <summary>
        /// The Size (coordinate space) revealed to Children.
        /// </summary>
        internal virtual Vector2 ChildSize => base.Size - new Vector2(Padding.TotalHorizontal, Padding.TotalVertical);

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        internal virtual Vector2 ChildScale => Vector2.One;

        /// <summary>
        /// Offset which is only applied to Children.
        /// </summary>
        internal virtual Vector2 ChildOffset => new Vector2(Padding.Left + Margin.Left, Padding.Top + Margin.Top);

        //Because of our custom DrawQuad implementation below, we want to expose the *base* DrawQuad when something requests our bounds.
        protected override Quad DrawQuadForBounds => base.DrawQuad;

        //Custom DrawQuad implementation excludes Margin/Padding.
        protected override Quad DrawQuad
        {
            get
            {
                Vector2 s = ChildSize;

                //most common use case gets a shortcut
                if (!FlipHorizontal && !FlipVertical) return new Quad(ChildOffset.X, ChildOffset.Y, s.X, s.Y);

                if (FlipHorizontal && FlipVertical)
                    return new Quad(s.X, s.Y, -s.X, -s.Y);
                if (FlipHorizontal)
                    return new Quad(s.X, 0, -s.X, s.Y);
                return new Quad(0, s.Y, s.X, -s.Y);
            }
        }

        /// <summary>
        /// Add a Drawable to Content's children list, recursing until Content == this.
        /// </summary>
        /// <param name="drawable">The drawable to be added.</param>
        public virtual void Add(Drawable drawable)
        {
            Debug.Assert(IsLoaded, "Can not add children before Container is loaded.");
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
        /// <param name="collection">The collection of drawables to be added.</param>
        public void Add(IEnumerable<Drawable> collection)
        {
            foreach (Drawable d in collection)
                Add(d);
        }

        /// <summary>
        /// Add a Drawable to this container's Children list, disregarding the value of Content.
        /// </summary>
        /// <param name="drawable">The drawable to be added.</param>
        protected void AddInternal(Drawable drawable)
        {
            Debug.Assert(drawable != null, "null-Drawables may not be added to Containers.");

            drawable.ChangeParent(this);

            if (!IsLoaded)
                pendingChildren.Add(drawable);
            else
                children.Add(drawable);
        }

        /// <summary>
        /// Add a collection of Drawables to this container's Children list, disregarding the value of Content.
        /// </summary>
        /// <param name="collection">The collection of drawables to be added.</param>
        protected void AddInternal(IEnumerable<Drawable> collection)
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

            if (dispose)
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

        protected internal override bool UpdateSubTree()
        {
            if (!base.UpdateSubTree()) return false;

            UpdateChildrenLife();

            foreach (Drawable child in children.AliveItems)
                child.UpdateSubTree();

            UpdateLayout();
            return true;
        }

        public override void Load(BaseGame game)
        {
            base.Load(game);

            this.game = game;

            if (pendingChildrenInternal != null)
            {
                AddInternal(pendingChildren);
                pendingChildrenInternal = null;
            }
        }

        private void loadChild(Drawable obj)
        {
            obj.Load(game);
        }

        internal virtual void InvalidateFromChild(Invalidation invalidation, Drawable source) { }

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

        /// <summary>
        /// Perform any layout changes just before autosize is calculated.		
        /// </summary>		
        protected virtual void UpdateLayout()
        {
        }

        protected internal override DrawNode GenerateDrawNodeSubtree(DrawNode node = null)
        {
            ContainerDrawNode cNode = base.GenerateDrawNodeSubtree(node) as ContainerDrawNode;

            if (children.AliveItems.Count > 0)
            {
                if (cNode.Children != null)
                {
                    var current = children.AliveItems;
                    var target = cNode.Children;

                    int j = 0;
                    foreach (Drawable drawable in current)
                    {
                        if (!drawable.IsVisible) continue;

                        //todo: make this more efficient.
                        if (game?.ScreenSpaceDrawQuad.FastIntersects(drawable.ScreenSpaceDrawQuad) == false)
                            continue;

                        if (j < target.Count && target[j].Drawable == drawable)
                        {
                            drawable.GenerateDrawNodeSubtree(target[j]);
                        }
                        else
                        {
                            if (j < target.Count)
                                target.RemoveAt(j);
                            target.Insert(j, drawable.GenerateDrawNodeSubtree());
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
                cNode?.Children?.Clear();

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

        public override void Flush(bool propagateChildren = false, Type flushType = null)
        {
            base.Flush(propagateChildren, flushType);

            if (propagateChildren)
                foreach (var c in children) c.Flush(true, flushType);
        }

        public override Drawable DelayReset()
        {
            base.DelayReset();
            foreach (var c in children) c.DelayReset();

            return this;
        }

        public override bool Contains(Vector2 screenSpacePos)
        {
            if (!Masking || CornerRadius == 0.0f)
                return base.Contains(screenSpacePos);

            Vector2 localSpacePos = GetLocalPosition(screenSpacePos);
            Primitives.RectangleF aabb = DrawQuad.AABBf;

            /*Vector2 scale = Scale * (Parent?.ChildScale ?? Vector2.One);
            aabb.X *= scale.X;
            aabb.Y *= scale.Y;
            aabb.Width *= scale.X;
            aabb.Height *= scale.Y;*/

            aabb.X += CornerRadius;
            aabb.Y += CornerRadius;
            aabb.Width -= 2 * CornerRadius;
            aabb.Height -= 2 * CornerRadius;

            return aabb.DistanceSquared(localSpacePos) < CornerRadius * CornerRadius;
        }
    }
}
