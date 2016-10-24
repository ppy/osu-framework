// Copyright (c) 2007-2016 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using OpenTK;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.OpenGL;
using OpenTK.Graphics;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Sprites;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added externally.
    /// </summary>
    public partial class Container : ShadedDrawable
    {
        private bool masking = false;
        public bool Masking
        {
            get { return masking; }
            set
            {
                if (masking == value)
                    return;

                masking = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float cornerRadius = 0.0f;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines how large of a radius is masked away around the corners.
        /// </summary>
        public virtual float CornerRadius
        {
            get { return cornerRadius; }
            set
            {
                if (cornerRadius == value)
                    return;

                cornerRadius = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float borderThickness = 0.0f;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines how thick of a border to draw around masked children _within_ the masked region.
        /// </summary>
        public virtual float BorderThickness
        {
            get { return borderThickness; }
            set
            {
                if (borderThickness == value)
                    return;

                borderThickness = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4 borderColour = Color4.Black;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines the color of the drawn border.
        /// </summary>
        public virtual Color4 BorderColour
        {
            get { return borderColour; }
            set
            {
                if (borderColour.Equals(value))
                    return;

                borderColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private float glowRadius = 0.0f;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines how large of a glow to draw _around_ the masked region.
        /// </summary>
        public virtual float GlowRadius
        {
            get { return glowRadius; }
            set
            {
                if (glowRadius == value)
                    return;

                glowRadius = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        private Color4 glowColour = Color4.Transparent;

        /// <summary>
        /// Only has an effect when Masking == true.
        /// Determines the color of the glow.
        /// </summary>
        public virtual Color4 GlowColour
        {
            get { return glowColour; }
            set
            {
                if (glowColour.Equals(value))
                    return;

                glowColour = value;
                Invalidate(Invalidation.DrawNode);
            }
        }

        protected override DrawNode CreateDrawNode() => new ContainerDrawNode();
        protected override bool IsCompatibleDrawNode(DrawNode node) => node.GetType() == typeof(ContainerDrawNode);

        protected override void ApplyDrawNode(DrawNode node)
        {
            ContainerDrawNode n = node as ContainerDrawNode;

            n.MaskingInfo = !Masking ? (MaskingInfo?)null : new MaskingInfo
            {
                ScreenSpaceAABB = ScreenSpaceDrawQuad.AABB,
                MaskingRect = DrawRectangle.Shrink(Margin),
                ToMaskingSpace = DrawInfo.MatrixInverse,
                CornerRadius = this.CornerRadius,
                BorderThickness = this.BorderThickness,
                BorderColour = this.BorderColour,
            };

            n.GlowRadius = GlowRadius;
            n.GlowColour = GlowColour;
            n.ScreenSpaceMaskingQuad = null;

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

        public virtual IEnumerable<Drawable> InternalChildren
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
                    c.Invalidate(Invalidation.Geometry);
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

        public override Vector2 DrawSize => base.DrawSize + new Vector2(Margin.TotalHorizontal, Margin.TotalVertical);

        /// <summary>
        /// The Size (coordinate space) revealed to Children.
        /// </summary>
        internal virtual Vector2 ChildSize => base.DrawSize - new Vector2(Padding.TotalHorizontal, Padding.TotalVertical);

        /// <summary>
        /// Scale which is only applied to Children.
        /// </summary>
        internal virtual Vector2 ChildScale => Vector2.One;

        /// <summary>
        /// Offset which is only applied to Children.
        /// </summary>
        internal virtual Vector2 ChildOffset => new Vector2(Padding.Left + Margin.Left, Padding.Top + Margin.Top);


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

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.Geometry, drawable);
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

        public virtual bool Remove(Drawable drawable, bool dispose = false)
        {
            if (drawable == null)
                return false;

            if (Content != this)
                return Content.Remove(drawable, dispose);

            bool result = children.Remove(drawable);
            drawable.Parent = null;

            if (!result) return false;

            if (dispose)
                drawable.Dispose();
            else
                drawable.Invalidate();

            if (AutoSizeAxes != Axes.None)
                InvalidateFromChild(Invalidation.Geometry, drawable);

            return true;
        }

        public int RemoveAll(Predicate<Drawable> match, bool dispose = false)
        {
            List<Drawable> toRemove = children.FindAll(match);
            for (int i = 0; i < toRemove.Count; i++)
                Remove(toRemove[i], dispose);

            return toRemove.Count;
        }

        public void Remove(IEnumerable<Drawable> range, bool dispose = false)
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

            if (AutoSizeAxes != Axes.None)
                updateAutoSize();
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

        internal virtual void InvalidateFromChild(Invalidation invalidation, Drawable source)
        {
            if (AutoSizeAxes == Axes.None) return;

            if ((invalidation & (Invalidation.Visibility | Invalidation.Geometry)) > 0)
                autoSize.Invalidate();
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
            bool changed = children.Update();

            if (changed && AutoSizeAxes != Axes.None)
                autoSize.Invalidate();

            return changed;
        }

        /// <summary>
        /// Perform any layout changes just before autosize is calculated.		
        /// </summary>		
        protected virtual void UpdateLayout()
        {
        }

        public override Axes RelativeSizeAxes
        {
            get { return base.RelativeSizeAxes; }
            set
            {
                Debug.Assert((AutoSizeAxes & value) == 0, "No axis can be relatively sized and automatically sized at the same time.");
                base.RelativeSizeAxes = value;
            }
        }

        private static void addFromContainer(ref int j, Container c, List<DrawNode> target, RectangleF childBounds)
        {
            List<Drawable> current = c.children.AliveItems;
            for (int i = 0; i < current.Count; ++i)
            {
                Drawable drawable = current[i];

                Container container = drawable as Container;
                if (container != null && !container.Masking)
                {
                    addFromContainer(ref j, container, target, childBounds);
                    continue;
                }

                DrawNode previous = j < target.Count ? target[j] : null;
                DrawNode next = drawable.GenerateDrawNodeSubtree(childBounds, previous);

                if (next == null)
                    continue;

                if (j < target.Count)
                    target[j] = next;
                else
                    target.Add(next);

                j++;
            }
        }

        protected internal override DrawNode GenerateDrawNodeSubtree(RectangleF bounds, DrawNode node = null)
        {
            // No need for a draw node at all if there are no children and we are not glowing.
            if (children.AliveItems.Count == 0 && (!Masking || GlowRadius == 0.0f))
                return null;

            ContainerDrawNode cNode = base.GenerateDrawNodeSubtree(bounds, node) as ContainerDrawNode;
            if (cNode == null)
                return null;

            RectangleF childBounds = bounds;
            if (Masking)
                childBounds.Intersect(ScreenSpaceDrawQuad.AABBf);

            if (cNode.Children == null)
                cNode.Children = new List<DrawNode>(children.AliveItems.Count);

            List<DrawNode> target = cNode.Children;

            int j = 0;
            addFromContainer(ref j, this, target, childBounds);

            if (j < target.Count)
                target.RemoveRange(j, target.Count - j);

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
            float cornerRadius = CornerRadius;
            Vector2 localPos = GetLocalPosition(screenSpacePos);
            RectangleF inputRect = DrawRectangle.Shrink(Margin);

            // Select a cheaper contains method when we don't need rounded edges.
            if (!Masking || cornerRadius == 0.0f)
                return inputRect.Contains(localPos);
            else
                return inputRect.Shrink(cornerRadius).DistanceSquared(localPos) <= cornerRadius * cornerRadius;
        }

        protected override RectangleF BoundingBox
        {
            get
            {
                if (!Masking || CornerRadius == 0.0f)
                    return base.BoundingBox;

                float cornerRadius = CornerRadius;
                RectangleF drawRect = DrawRectangle.Shrink(cornerRadius);

                Vector2 offset = ToParentSpace(Vector2.Zero);
                Vector2 u = ToParentSpace(new Vector2(cornerRadius, 0)) - offset;
                Vector2 v = ToParentSpace(new Vector2(0, cornerRadius)) - offset;

                Vector2 inflation = new Vector2((float)Math.Sqrt(u.X * u.X + v.X * v.X), (float)Math.Sqrt(u.Y * u.Y + v.Y * v.Y));
                return ToParentSpace(drawRect).AABBf.Inflate(inflation);
            }
        }
    }
}
