// Copyright (c) 2007-2017 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using osu.Framework.Lists;
using System.Collections.Generic;
using System;
using osu.Framework.Allocation;
using System.Threading.Tasks;
using osu.Framework.Extensions.TypeExtensions;

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
    public class Container<T> : ContainerBase, IContainerEnumerable<T>, IContainerCollection<T>
        where T : Drawable
    {
        /// <summary>
        /// Contructs a container that stores its children in a given <see cref="LifetimeList{T}"/>.
        /// If null is provides, then a new <see cref="LifetimeList{T}"/> is automatically created.
        /// </summary>
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

        protected Task LoadComponentAsync<TLoadable>(TLoadable component, Action<TLoadable> onLoaded = null) where TLoadable : Drawable => component.LoadAsync(game, this, onLoaded);

        /// <summary>
        /// The content of this container. <see cref="Children"/> and all methods that mutate
        /// <see cref="Children"/> (e.g. <see cref="Add(T)"/> and <see cref="Remove(T)"/>) are
        /// forwarded to the content. By default a container's content is itself, in which case
        /// <see cref="Children"/> refers to <see cref="ContainerBase.InternalChildren"/>.
        /// This property is useful for containers that require internal children that should
        /// not be exposed to the outside world, e.g. <see cref="ScrollContainer"/>.
        /// </summary>
        protected virtual Container<T> Content => this;

        /// <summary>
        /// The publicly accessible list of children. Forwards to the children of <see cref="Content"/>.
        /// If <see cref="Content"/> is this container, then returns <see cref="ContainerBase.InternalChildren"/>.
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
        /// Gets or sets the only child of this container.
        /// </summary>
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
        public bool Contains(T drawable) => IndexOf(drawable) >= 0;

        /// <summary>
        /// Adds a child to this container. This amount to adding a child to <see cref="Content"/>'s
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
        public void Add(IEnumerable<T> range)
        {
            foreach (T d in range)
                Add(d);
        }

        protected internal override void AddInternal(Drawable drawable)
        {
            if (Content == this && !(drawable is T))
                throw new InvalidOperationException($"Only {typeof(T).ReadableName()} type drawables may be added to a container of type {GetType().ReadableName()} which does not redirect {nameof(Content)}.");

            base.AddInternal(drawable);
        }

        /// <summary>
        /// Removes a given child from this container.
        /// </summary>
        public void Remove(T drawable)
        {
            if (Content != this)
                Content.Remove(drawable);
            else
                RemoveInternal(drawable);
        }

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
        public void Remove(IEnumerable<T> range)
        {
            if (range == null)
                return;

            foreach (T p in range)
                Remove(p);
        }

        /// <summary>
        /// Removes all children.
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
        /// Changes the depth of a child. This affects ordering of children within this container.
        /// </summary>
        /// <param name="child">The child whose depth is to be changed.</param>
        /// <param name="newDepth">The new depth value to be set.</param>
        public void ChangeChildDepth(T child, float newDepth)
        {
            if (!Contains(child))
                throw new InvalidOperationException("Can not change depth of drawable which is not contained within this container.");

            Remove(child);
            child.Depth = newDepth;
            Add(child);
        }
    }
}
