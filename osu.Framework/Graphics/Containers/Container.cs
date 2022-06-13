// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Colour;
using osuTK;
using System.Collections;
using System.Diagnostics;
using osu.Framework.Graphics.Effects;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added to it. Transformations applied to
    /// a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the
    /// generic version <see cref="Container{T}"/>.
    /// </summary>
    public class Container : Container<Drawable>
    {
    }

    /// <summary>
    /// A drawable which can have children added to it. Transformations applied to
    /// a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect,
    /// padding, and automatic sizing depending on their children.
    /// </summary>
    public class Container<T> : CompositeDrawable, IContainerEnumerable<T>, IContainerCollection<T>, ICollection<T>, IReadOnlyList<T>
        where T : Drawable
    {
        /// <summary>
        /// Constructs a <see cref="Container"/> that stores children.
        /// </summary>
        public Container()
        {
            if (typeof(T) == typeof(Drawable))
                internalChildrenAsT = (IReadOnlyList<T>)InternalChildren;
            else
                internalChildrenAsT = new LazyList<Drawable, T>(InternalChildren, c => (T)c);

            if (typeof(T) == typeof(Drawable))
                aliveInternalChildrenAsT = (IReadOnlyList<T>)AliveInternalChildren;
            else
                aliveInternalChildrenAsT = new LazyList<Drawable, T>(AliveInternalChildren, c => (T)c);
        }

        /// <summary>
        /// The content of this container. <see cref="Children"/> and all methods that mutate
        /// <see cref="Children"/> (e.g. <see cref="Add(T)"/> and <see cref="Remove(T)"/>) are
        /// forwarded to the content. By default a container's content is itself, in which case
        /// <see cref="Children"/> refers to <see cref="CompositeDrawable.InternalChildren"/>.
        /// This property is useful for containers that require internal children that should
        /// not be exposed to the outside world, e.g. <see cref="ScrollContainer{T}"/>.
        /// </summary>
        protected virtual Container<T> Content => this;

        /// <summary>
        /// The publicly accessible list of children. Forwards to the children of <see cref="Content"/>.
        /// If <see cref="Content"/> is this container, then returns <see cref="CompositeDrawable.InternalChildren"/>.
        /// Assigning to this property will dispose all existing children of this Container.
        /// <remarks>
        /// If a foreach loop is used, iterate over the <see cref="Container"/> directly rather than its <see cref="Children"/>.
        /// </remarks>
        /// </summary>
        public IReadOnlyList<T> Children
        {
            get
            {
                if (Content != this)
                    return Content.Children;

                return internalChildrenAsT;
            }
            set => ChildrenEnumerable = value;
        }

        /// <summary>
        /// The publicly accessible list of alive children. Forwards to the alive children of <see cref="Content"/>.
        /// If <see cref="Content"/> is this container, then returns <see cref="CompositeDrawable.AliveInternalChildren"/>.
        /// </summary>
        public IReadOnlyList<T> AliveChildren
        {
            get
            {
                if (Content != this)
                    return Content.AliveChildren;

                return aliveInternalChildrenAsT;
            }
        }

        /// <summary>
        /// Accesses the <paramref name="index"/>-th child.
        /// </summary>
        /// <param name="index">The index of the child to access.</param>
        /// <returns>The <paramref name="index"/>-th child.</returns>
        public T this[int index] => Children[index];

        /// <summary>
        /// The amount of elements in <see cref="Children"/>.
        /// </summary>
        public int Count => Children.Count;

        /// <summary>
        /// Whether this <see cref="Container{T}"/> can have elements added and removed. Always false.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Copies the elements of the <see cref="Container{T}"/> to an Array, starting at a particular Array index.
        /// </summary>
        /// <param name="array">The Array into which all children should be copied.</param>
        /// <param name="arrayIndex">The starting index in the Array.</param>
        public void CopyTo(T[] array, int arrayIndex)
        {
            foreach (var c in Children)
                array[arrayIndex++] = c;
        }

        public Enumerator GetEnumerator() => new Enumerator(this);

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Sets all children of this container to the elements contained in the enumerable.
        /// </summary>
        public IEnumerable<T> ChildrenEnumerable
        {
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Children cannot be mutated on a disposed drawable.");

                Clear();
                AddRange(value);
            }
        }

        /// <summary>
        /// Gets or sets the only child of this container.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public T Child
        {
            get
            {
                if (Children.Count != 1)
                    throw new InvalidOperationException($"Cannot call {nameof(InternalChild)} unless there's exactly one {nameof(Drawable)} in {nameof(Children)} (currently {Children.Count})!");

                return Children[0];
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(ToString(), "Children cannot be mutated on a disposed drawable.");

                Clear();
                Add(value);
            }
        }

        private readonly IReadOnlyList<T> internalChildrenAsT;
        private readonly IReadOnlyList<T> aliveInternalChildrenAsT;

        /// <summary>
        /// The index of a given child within <see cref="Children"/>.
        /// </summary>
        /// <returns>
        /// If the child is found, its index. Otherwise, the negated index it would obtain
        /// if it were added to <see cref="Children"/>.
        /// </returns>
        public int IndexOf(T drawable)
        {
            if (Content != this)
                return Content.IndexOf(drawable);

            return IndexOfInternal(drawable);
        }

        /// <summary>
        /// Checks whether a given child is contained within <see cref="Children"/>.
        /// </summary>
        public bool Contains(T drawable)
        {
            int index = IndexOf(drawable);
            return index >= 0 && this[index] == drawable;
        }

        /// <summary>
        /// Adds a child to this container. This amounts to adding a child to <see cref="Content"/>'s
        /// <see cref="Children"/>, recursing until <see cref="Content"/> == this.
        /// </summary>
        public virtual void Add(T drawable)
        {
            if (drawable == Content)
                throw new InvalidOperationException("Content may not be added to itself.");

            if (Content == this)
                AddInternal(drawable);
            else
                Content.Add(drawable);
        }

        /// <summary>
        /// Adds a range of children. This is equivalent to calling <see cref="Add(T)"/> on
        /// each element of the range in order.
        /// </summary>
        public void AddRange(IEnumerable<T> range)
        {
            if (range is IContainerEnumerable<Drawable>)
            {
                throw new InvalidOperationException($"Attempting to add a {nameof(IContainer)} as a range of children to {this}."
                                                    + $"If intentional, consider using the {nameof(IContainerEnumerable<Drawable>.Children)} property instead.");
            }

            foreach (T d in range)
                Add(d);
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            if (Content == this && drawable != null && !(drawable is T))
                throw new InvalidOperationException($"Only {typeof(T).ReadableName()} type drawables may be added to a container of type {GetType().ReadableName()} which does not redirect {nameof(Content)}.");

            base.AddInternal(drawable);
        }

        /// <summary>
        /// Removes a given child from this container.
        /// </summary>
        public virtual bool Remove(T drawable) => Content != this ? Content.Remove(drawable) : RemoveInternal(drawable);

        /// <summary>
        /// Removes all children which match the given predicate.
        /// This is equivalent to calling <see cref="Remove(T)"/> for each child that
        /// matches the given predicate.
        /// </summary>
        /// <returns>The amount of removed children.</returns>
        public int RemoveAll(Predicate<T> pred)
        {
            if (Content != this)
                return Content.RemoveAll(pred);

            int removedCount = 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var tChild = (T)InternalChildren[i];

                if (pred.Invoke(tChild))
                {
                    RemoveInternal(tChild);
                    removedCount++;
                    i--;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Removes a range of children. This is equivalent to calling <see cref="Remove(T)"/> on
        /// each element of the range in order.
        /// </summary>
        public void RemoveRange(IEnumerable<T> range)
        {
            if (range == null)
                return;

            foreach (T p in range)
                Remove(p);
        }

        /// <summary>
        /// Removes all children.
        /// </summary>
        public void Clear() => Clear(true);

        /// <summary>
        /// Removes all children.
        /// </summary>
        /// <param name="disposeChildren">
        /// Whether removed children should also get disposed.
        /// Disposal will be recursive.
        /// </param>
        public virtual void Clear(bool disposeChildren)
        {
            if (Content != null && Content != this)
                Content.Clear(disposeChildren);
            else
                ClearInternal(disposeChildren);
        }

        /// <summary>
        /// Changes the depth of a child. This affects ordering of children within this container.
        /// </summary>
        /// <param name="child">The child whose depth is to be changed.</param>
        /// <param name="newDepth">The new depth value to be set.</param>
        public void ChangeChildDepth(T child, float newDepth)
        {
            if (Content != this)
                Content.ChangeChildDepth(child, newDepth);
            else
                ChangeInternalChildDepth(child, newDepth);
        }

        /// <summary>
        /// If enabled, only the portion of children that falls within this <see cref="Container"/>'s
        /// shape is drawn to the screen.
        /// </summary>
        public new bool Masking
        {
            get => base.Masking;
            set => base.Masking = value;
        }

        /// <summary>
        /// Determines over how many pixels the alpha component smoothly fades out when an inner <see cref="EdgeEffect"/> or <see cref="BorderThickness"/> is present.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new float MaskingSmoothness
        {
            get => base.MaskingSmoothness;
            set => base.MaskingSmoothness = value;
        }

        /// <summary>
        /// Determines how large of a radius is masked away around the corners.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new float CornerRadius
        {
            get => base.CornerRadius;
            set => base.CornerRadius = value;
        }

        /// <summary>
        /// Determines how gentle the curve of the corner straightens. A value of 2 results in
        /// circular arcs, a value of 2.5 (default) results in something closer to apple's "continuous corner".
        /// Values between 2 and 10 result in varying degrees of "continuousness", where larger values are smoother.
        /// Values between 1 and 2 result in a "flatter" appearance than round corners.
        /// Values between 0 and 1 result in a concave, round corner as opposed to a convex round corner,
        /// where a value of 0.5 is a circular concave arc.
        /// Only has an effect when <see cref="Masking"/> is true and <see cref="CornerRadius"/> is non-zero.
        /// </summary>
        public new float CornerExponent
        {
            get => base.CornerExponent;
            set => base.CornerExponent = value;
        }

        /// <summary>
        /// Determines how thick of a border to draw around the inside of the masked region.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// The border only is drawn on top of children using a sprite shader.
        /// </summary>
        /// <remarks>
        /// Drawing borders is optimized heavily into our sprite shaders. As a consequence
        /// borders are only drawn correctly on top of quad-shaped children using our sprite
        /// shaders.
        /// </remarks>
        public new float BorderThickness
        {
            get => base.BorderThickness;
            set => base.BorderThickness = value;
        }

        /// <summary>
        /// Determines the color of the border controlled by <see cref="BorderThickness"/>.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new ColourInfo BorderColour
        {
            get => base.BorderColour;
            set => base.BorderColour = value;
        }

        /// <summary>
        /// Determines an edge effect of this <see cref="Container"/>.
        /// Edge effects are e.g. glow or a shadow.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new EdgeEffectParameters EdgeEffect
        {
            get => base.EdgeEffect;
            set => base.EdgeEffect = value;
        }

        /// <summary>
        /// Shrinks the space children may occupy within this <see cref="Container"/>
        /// by the specified amount on each side.
        /// </summary>
        public new MarginPadding Padding
        {
            get => base.Padding;
            set => base.Padding = value;
        }

        /// <summary>
        /// Whether to use a local vertex batch for rendering. If false, a parenting vertex batch will be used.
        /// </summary>
        public new bool ForceLocalVertexBatch
        {
            get => base.ForceLocalVertexBatch;
            set => base.ForceLocalVertexBatch = value;
        }

        /// <summary>
        /// The size of the relative position/size coordinate space of children of this <see cref="Container"/>.
        /// Children positioned at this size will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.One"/> in this <see cref="Container"/>.
        /// </summary>
        public new Vector2 RelativeChildSize
        {
            get => base.RelativeChildSize;
            set => base.RelativeChildSize = value;
        }

        /// <summary>
        /// The offset of the relative position/size coordinate space of children of this <see cref="Container"/>.
        /// Children positioned at this offset will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.Zero"/> in this <see cref="Container"/>.
        /// </summary>
        public new Vector2 RelativeChildOffset
        {
            get => base.RelativeChildOffset;
            set => base.RelativeChildOffset = value;
        }

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. <see cref="CompositeDrawable.InternalChildren"/>.
        /// Children's <see cref="Drawable.BypassAutoSizeAxes"/> are ignored for automatic sizing.
        /// Most notably, <see cref="Drawable.RelativePositionAxes"/> and <see cref="Drawable.RelativeSizeAxes"/> of children
        /// do not affect automatic sizing to avoid circular size dependencies.
        /// It is not allowed to manually set <see cref="Drawable.Size"/> (or <see cref="Drawable.Width"/> / <see cref="Drawable.Height"/>)
        /// on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        public new Axes AutoSizeAxes
        {
            get => base.AutoSizeAxes;
            set => base.AutoSizeAxes = value;
        }

        /// <summary>
        /// The duration which automatic sizing should take. If zero, then it is instantaneous.
        /// Otherwise, this is equivalent to applying an automatic size via a resize transform.
        /// </summary>
        public new float AutoSizeDuration
        {
            get => base.AutoSizeDuration;
            set => base.AutoSizeDuration = value;
        }

        /// <summary>
        /// The type of easing which should be used for smooth automatic sizing when <see cref="AutoSizeDuration"/>
        /// is non-zero.
        /// </summary>
        public new Easing AutoSizeEasing
        {
            get => base.AutoSizeEasing;
            set => base.AutoSizeEasing = value;
        }

        public struct Enumerator : IEnumerator<T>
        {
            private Container<T> container;
            private int currentIndex;

            internal Enumerator(Container<T> container)
            {
                this.container = container;
                currentIndex = -1; // The first MoveNext() should bring the iterator to 0
            }

            public bool MoveNext() => ++currentIndex < container.Count;

            public void Reset() => currentIndex = -1;

            public readonly T Current => container[currentIndex];

            readonly object IEnumerator.Current => Current;

            public void Dispose()
            {
                container = null;
            }
        }
    }
}
