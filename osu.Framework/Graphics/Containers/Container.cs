// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using OpenTK;
using OpenTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Extensions.TypeExtensions;
using osu.Framework.Graphics.Colour;
using osu.Framework.Lists;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A drawable which can have children added to it.
    /// Transformations applied to a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect, padding, and automatic sizing depending on their children.
    /// If all children are of a specific non-<see cref="Drawable"/> type, use the generic version: <see cref="Container{T}"/>.
    /// </summary>
    public class Container : Container<Drawable>
    {
    }

    /// <summary>
    /// A drawable which can have children added to it.
    /// Transformations applied to a container are also applied to its children.
    /// Additionally, containers support various effects, such as masking, edge effect, padding, and automatic sizing depending on their children.
    /// </summary>
    /// <typeparam name="T">The subtype of <see cref="Drawable"/> which all children in this container must derive from.</typeparam>
    public class Container<T> : CompositeDrawable, IContainerEnumerable<T>, IContainerCollection<T>
        where T : Drawable
    {
        /// <summary>
        /// Constructs a <see cref="Container{T}"/> that stores its children in a given <see cref="LifetimeList{T}"/>.
        /// If <paramref name="lifetimeList"/> is null, a new <see cref="LifetimeList{T}"/> is created.
        /// </summary>
        /// <param name="lifetimeList">The <see cref="LifetimeList{T}"/> to keep the container's children.</param>
        public Container(LifetimeList<Drawable> lifetimeList = null) : base(lifetimeList)
        {
            if (typeof(T) == typeof(Drawable))
                internalChildrenAsT = (IReadOnlyList<T>)InternalChildren;
            else
                internalChildrenAsT = new LazyList<Drawable, T>(InternalChildren, c => (T)c);
        }

        private Game game;

        [BackgroundDependencyLoader(true)]
        private void load(Game game)
        {
            this.game = game;
        }

        protected Task LoadComponentAsync<TLoadable>(TLoadable component, Action<TLoadable> onLoaded = null) where TLoadable : Drawable =>
            component.LoadAsync(game, this, onLoaded);

        /// <summary>
        /// The content of this container.
        /// <see cref="Children"/> and all methods that mutate <see cref="Children"/>
        /// (e.g. <see cref="Add(T)"/> and <see cref="Remove(T)"/>) are forwarded to the content.
        /// By default a container's content is itself,
        /// in which case <see cref="Children"/> refers to <see cref="CompositeDrawable.InternalChildren"/>.
        /// This property is useful for containers that require internal children that should
        /// not be exposed to the outside world, e.g. <see cref="ScrollContainer"/>.
        /// </summary>
        protected virtual Container<T> Content => this;

        /// <summary>
        /// The publicly accessible list of children.
        /// Forwards to the children of <see cref="Content"/>.
        /// If <see cref="Content"/> is this container, then returns <see cref="CompositeDrawable.InternalChildren"/>.
        /// Assigning to this property will dispose all existing children of this Container.
        /// </summary>
        public IReadOnlyList<T> Children
        {
            get
            {
                if (Content != this)
                    return Content.Children;

                return internalChildrenAsT;
            }
            set
            {
                ChildrenEnumerable = value;
            }
        }

        /// <summary>
        /// Sets all children of this container to the elements contained in the enumerable.
        /// </summary>
        public IEnumerable<T> ChildrenEnumerable
        {
            set
            {
                Clear();
                Add(value);
            }
        }

        /// <summary>
        /// Gets or sets the only child in this <see cref="Container{T}"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">There only has to be one child in order to access the property getter.</exception>
        public T Child
        {
            get
            {
                if (Children.Count != 1)
                    throw new InvalidOperationException($"{nameof(Child)} is only available when there's only 1 in {nameof(Children)}!");

                return Children[0];
            }
            set
            {
                Clear();
                Add(value);
            }
        }

        private readonly IReadOnlyList<T> internalChildrenAsT;

        /// <summary>
        /// The index of a given child within <see cref="Children"/>.
        /// </summary>
        /// <param name="drawable">The drawable to search for.</param>
        /// <returns>
        /// If the child is found, its index.
        /// Otherwise, the negated index it would obtain if it were added to <see cref="Children"/>.
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
        /// <param name="drawable">The drawable to search for.</param>
        /// <returns>Whether <paramref name="drawable"/> was found within the internal children.</returns>
        public bool Contains(T drawable) => IndexOf(drawable) >= 0;

        /// <summary>
        /// Adds a child to <see cref="Children"/>.
        /// This method will recurse until <see cref="Content"/> == this.
        /// </summary>
        /// <param name="drawable">
        /// The drawable to add.
        /// This must not be added to any other containers while within this container.
        /// </param>
        /// <exception cref="InvalidOperationException">Content cannot be added to itself.</exception>
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
        /// Adds a collection of children to <see cref="Children"/>.
        /// This is equivalent to calling <see cref="Add(IEnumerable{T})"/> on each element of the collection in order.
        /// </summary>
        /// <param name="collection">The collection of drawables to add.</param>
        public void Add(IEnumerable<T> collection)
        {
            foreach (T d in collection)
                Add(d);
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            if (Content == this && !(drawable is T))
                throw new InvalidOperationException($"Only {typeof(T).ReadableName()} type drawables may be added to a container of type {GetType().ReadableName()} which does not redirect {nameof(Content)}.");

            base.AddInternal(drawable);
        }

        /// <summary>
        /// Removes a given child from <see cref="Children"/>.
        /// </summary>
        /// <param name="drawable">The drawable to remove.</param>
        public void Remove(T drawable)
        {
            if (Content != this)
                Content.Remove(drawable);
            else
                RemoveInternal(drawable);
        }

        /// <summary>
        /// Removes all children which match the given predicate.
        /// This is equivalent to calling <see cref="Remove(T)"/> for each child that matches the given predicate.
        /// </summary>
        /// <param name="match">The condition to be satisfied when removing each child.</param>
        /// <returns>The number of children removed.</returns>
        public int RemoveAll(Predicate<T> match)
        {
            if (Content != this)
                return Content.RemoveAll(match);

            int removedCount = 0;

            for (int i = 0; i < InternalChildren.Count; i++)
            {
                var tChild = (T)InternalChildren[i];

                if (match.Invoke(tChild))
                {
                    RemoveInternal(tChild);
                    removedCount++;
                    i--;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Removes a range of children.
        /// This is equivalent to calling <see cref="Remove(T)"/> on each element of the range in order.
        /// </summary>
        /// <param name="range">The range of drawables to remove.</param>
        public void Remove(IEnumerable<T> range)
        {
            if (range == null)
                return;

            foreach (T p in range)
                Remove(p);
        }

        /// <summary>
        /// Clears this container by removing all children.
        /// </summary>
        /// <param name="disposeChildren">
        /// Whether removed children should also get disposed.
        /// Disposal will be recursive.
        /// </param>
        public virtual void Clear(bool disposeChildren = true)
        {
            if (Content != null && Content != this)
                Content.Clear(disposeChildren);
            else
                ClearInternal(disposeChildren);
        }

        /// <summary>
        /// Changes the depth of a child.
        /// This affects the ordering of <see cref="Children"/>.
        /// </summary>
        /// <remarks>
        /// This method actually simply removes the specified drawable internally and adds it back,
        /// updating the position of its actual index within the list.
        /// </remarks>
        /// <param name="child">The child whose depth is to be changed.</param>
        /// <param name="newDepth">The new depth value to be set.</param>
        /// <exception cref="InvalidOperationException">Cannot change depth of drawable which is not contained within the container.</exception>
        public void ChangeChildDepth(T child, float newDepth)
        {
            if (!Contains(child))
                throw new InvalidOperationException("Can not change depth of drawable which is not contained within this container.");

            Remove(child);
            child.Depth = newDepth;
            Add(child);
        }

        /// <summary>
        /// Fades the current <see cref="EdgeEffect"/> to the specified alpha value.
        /// </summary>
        /// <param name="newAlpha">The new alpha value of the edge effect.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        public new void FadeEdgeEffectTo(float newAlpha, double duration = 0, EasingTypes easing = EasingTypes.None) => base.FadeEdgeEffectTo(newAlpha, duration, easing);

        /// <summary>
        /// Fades the current <see cref="EdgeEffect"/> to the specified colour value.
        /// </summary>
        /// <param name="newColour">The new colour value of the edge effect.</param>
        /// <param name="duration">The transform duration.</param>
        /// <param name="easing">The transform easing.</param>
        public new void FadeEdgeEffectTo(Color4 newColour, double duration = 0, EasingTypes easing = EasingTypes.None) => base.FadeEdgeEffectTo(newColour, duration, easing);

        /// <summary>
        /// If enabled, only the portion of children that falls within this
        /// <see cref="Container{T}"/>'s shape is drawn to the screen.
        /// </summary>
        public new bool Masking
        {
            get { return base.Masking; }
            set { base.Masking = value; }
        }

        /// <summary>
        /// Determines over how many pixels the alpha component smoothly fades out.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new float MaskingSmoothness
        {
            get { return base.MaskingSmoothness; }
            set { base.MaskingSmoothness = value; }
        }

        /// <summary>
        /// Determines how large of a radius is masked away around the corners.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new float CornerRadius
        {
            get { return base.CornerRadius; }
            set { base.CornerRadius = value; }
        }

        /// <summary>
        /// Determines how thick of a border to draw around the inside of the masked region.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// The border only is drawn on top of children using a sprite shader.
        /// </summary>
        /// <remarks>
        /// Drawing borders is optimized heavily into our sprite shaders.
        /// As a consequence borders are only drawn correctly on top of quad-shaped children using our sprite shaders.
        /// </remarks>
        public new float BorderThickness
        {
            get { return base.BorderThickness; }
            set { base.BorderThickness = value; }
        }

        /// <summary>
        /// Determines the color of the border controlled by <see cref="BorderThickness"/>.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new SRGBColour BorderColour
        {
            get { return base.BorderColour; }
            set { base.BorderColour = value; }
        }

        /// <summary>
        /// Determines an edge effect of this <see cref="Container{T}"/>.
        /// Edge effects are e.g. glow or a shadow.
        /// Only has an effect when <see cref="Masking"/> is true.
        /// </summary>
        public new EdgeEffectParameters EdgeEffect
        {
            get { return base.EdgeEffect; }
            set { base.EdgeEffect = value; }
        }

        /// <summary>
        /// Shrinks the space children may occupy within this <see cref="Container{T}"/>
        /// by the specified amount on each side.
        /// </summary>
        public new MarginPadding Padding
        {
            get { return base.Padding; }
            set { base.Padding = value; }
        }

        /// <summary>
        /// The size of the relative position/size coordinate space of children of this <see cref="Container{T}"/>.
        /// Children positioned at this size will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.One"/> in this <see cref="Container{T}"/>.
        /// </summary>
        public new Vector2 RelativeChildSize
        {
            get { return base.RelativeChildSize; }
            set { base.RelativeChildSize = value; }
        }

        /// <summary>
        /// The offset of the relative position/size coordinate space of children of this <see cref="Container{T}"/>.
        /// Children positioned at this offset will appear as if they were positioned at <see cref="Drawable.Position"/> = <see cref="Vector2.Zero"/> in this <see cref="Container{T}"/>.
        /// </summary>
        public new Vector2 RelativeChildOffset
        {
            get { return base.RelativeChildOffset; }
            set { base.RelativeChildOffset = value; }
        }

        /// <summary>
        /// Tweens the <see cref="RelativeChildSize"/> of this <see cref="Container{T}"/>.
        /// </summary>
        /// <param name="newSize">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public new void TransformRelativeChildSizeTo(Vector2 newSize, double duration = 0, EasingTypes easing = EasingTypes.None) => base.TransformRelativeChildSizeTo(newSize, duration, easing);

        /// <summary>
        /// Tweens the <see cref="RelativeChildOffset"/> of this <see cref="Container{T}"/>.
        /// </summary>
        /// <param name="newOffset">The coordinate space to tween to.</param>
        /// <param name="duration">The tween duration.</param>
        /// <param name="easing">The tween easing.</param>
        public new void TransformRelativeChildOffsetTo(Vector2 newOffset, double duration = 0, EasingTypes easing = EasingTypes.None) => base.TransformRelativeChildOffsetTo(newOffset, duration, easing);

        /// <summary>
        /// Controls which <see cref="Axes"/> are automatically sized w.r.t. <see cref="CompositeDrawable.internalChildren"/>.
        /// Children's <see cref="Drawable.BypassAutoSizeAxes"/> are ignored for automatic sizing.
        /// Most notably, <see cref="Drawable.RelativePositionAxes"/> and <see cref="RelativeSizeAxes"/> of children do not affect automatic sizing to avoid circular size dependencies.
        /// It is not allowed to manually set <see cref="Drawable.Size"/> (or <see cref="Drawable.Width"/> / <see cref="Drawable.Height"/>) on any <see cref="Axes"/> which are automatically sized.
        /// </summary>
        /// <exception cref="InvalidOperationException">No axis can be relatively sized and automatically sized at the same time.</exception>
        public new Axes AutoSizeAxes
        {
            get { return base.AutoSizeAxes; }
            set { base.AutoSizeAxes = value; }
        }

        /// <summary>
        /// The duration which automatic sizing should take. If zero, then it is instantaneous.
        /// Otherwise, this is equivalent to applying an automatic size via <see cref="Drawable.ResizeTo(Vector2, double, EasingTypes)"/>.
        /// </summary>
        public new float AutoSizeDuration
        {
            get { return base.AutoSizeDuration; }
            set { base.AutoSizeDuration = value; }
        }

        /// <summary>
        /// The type of easing which should be used for smooth automatic sizing when <see cref="AutoSizeDuration"/> is non-zero.
        /// </summary>
        public new EasingTypes AutoSizeEasing
        {
            get { return base.AutoSizeEasing; }
            set { base.AutoSizeEasing = value; }
        }
    }
}
