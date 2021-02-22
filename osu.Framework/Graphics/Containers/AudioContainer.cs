// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Audio;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Effects;
using osuTK;

namespace osu.Framework.Graphics.Containers
{
    /// <summary>
    /// A container which exposes audio adjustments via <see cref="IAggregateAudioAdjustment"/>.
    /// </summary>
    /// <remarks>
    /// This is a bare-minimal implementation of a container, so it may be required to be nested inside a <see cref="Container"/> for some use cases.
    /// </remarks>
    /// <typeparam name="T">The type of <see cref="Drawable"/>.</typeparam>
    public class AudioContainer<T> : DrawableAudioWrapper, IContainerEnumerable<T>, IContainerCollection<T>, ICollection<T>, IReadOnlyList<T>
        where T : Drawable
    {
        private readonly Container<T> container;

        public AudioContainer()
            : this(new Container<T>())
        {
        }

        private AudioContainer(Container<T> container)
            : base(container)
        {
            this.container = container;
        }

        public override Vector2 Size
        {
            get => container.Size;
            set
            {
                base.Size = new Vector2(
                    RelativeSizeAxes.HasFlag(Axes.X) ? 1 : value.X,
                    RelativeSizeAxes.HasFlag(Axes.Y) ? 1 : value.Y);

                container.Size = value;
            }
        }

        public override Axes RelativeSizeAxes
        {
            get => container.RelativeSizeAxes;
            set
            {
                base.RelativeSizeAxes = value;
                container.RelativeSizeAxes = value;
            }
        }

        public new Axes AutoSizeAxes
        {
            get => container.AutoSizeAxes;
            set
            {
                base.AutoSizeAxes = value;
                container.AutoSizeAxes = value;
            }
        }

        public new EdgeEffectParameters EdgeEffect
        {
            get => base.EdgeEffect;
            set => base.EdgeEffect = value;
        }

        public new Vector2 RelativeChildSize
        {
            get => container.RelativeChildSize;
            set => container.RelativeChildSize = value;
        }

        public new Vector2 RelativeChildOffset
        {
            get => container.RelativeChildOffset;
            set => container.RelativeChildOffset = value;
        }

        public Container<T>.Enumerator GetEnumerator() => container.GetEnumerator();

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IReadOnlyList<T> Children
        {
            get => container.Children;
            set
            {
                unbindAllAdjustments();
                container.Children = value;
                bindAllAdjustments();
            }
        }

        public int RemoveAll(Predicate<T> match) => container.RemoveAll(d =>
        {
            if (!match(d))
                return false;

            unbindAdjustments(d);
            return true;
        });

        public T Child
        {
            get => container.Child;
            set
            {
                unbindAllAdjustments();
                container.Child = value;
                bindAllAdjustments();
            }
        }

        public IEnumerable<T> ChildrenEnumerable
        {
            set
            {
                unbindAllAdjustments();
                container.ChildrenEnumerable = value;
                bindAllAdjustments();
            }
        }

        public void Add(T drawable)
        {
            container.Add(drawable);
            bindAdjustments(drawable);
        }

        public void Clear()
        {
            unbindAllAdjustments();
            container.Clear();
        }

        public bool Contains(T item) => container.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) => container.CopyTo(array, arrayIndex);

        public void AddRange(IEnumerable<T> collection)
        {
            // For Container, AddRange() is equivalent to calling Add() with each drawable in the collection.
            foreach (var drawable in collection)
                Add(drawable);
        }

        public bool Remove(T drawable)
        {
            if (!container.Remove(drawable))
                return false;

            unbindAdjustments(drawable);
            return true;
        }

        int ICollection<T>.Count => container.Count;

        public bool IsReadOnly => container.IsReadOnly;

        public void RemoveRange(IEnumerable<T> range)
        {
            // For Container, RemoveRange() is equivalent to calling Remove() with each drawable in the collection.
            foreach (var drawable in range)
                Remove(drawable);
        }

        int IReadOnlyCollection<T>.Count => container.Count;

        public T this[int index] => container[index];

        #region Adjustment Binding

        /// <summary>
        /// Binds adjustments to a single <typeparamref name="T"/> object.
        /// </summary>
        private void bindAdjustments(T drawable)
        {
            if (drawable is IAdjustableAudioComponent adjustable)
                adjustable.BindAdjustments(this);
        }

        /// <summary>
        /// Binds adjustments to all container <typeparamref name="T"/> objects.
        /// </summary>
        private void bindAllAdjustments()
        {
            foreach (var adjustable in container.OfType<IAdjustableAudioComponent>())
                adjustable.BindAdjustments(this);
        }

        /// <summary>
        /// Unbinds adjustments from a single <typeparamref name="T"/> object.
        /// </summary>
        private void unbindAdjustments(T drawable)
        {
            if (drawable is IAdjustableAudioComponent adjustable)
                adjustable.UnbindAdjustments(this);
        }

        /// <summary>
        /// Unbinds adjustments from all contained <typeparamref name="T"/> objects.
        /// </summary>
        private void unbindAllAdjustments()
        {
            foreach (var adjustable in container.OfType<IAdjustableAudioComponent>())
                adjustable.UnbindAdjustments(this);
        }

        #endregion
    }

    /// <summary>
    /// A container which exposes audio adjustments via <see cref="IAggregateAudioAdjustment"/>.
    /// </summary>
    /// <remarks>
    /// This is a bare-minimal implementation of a container, so it may be required to be nested inside a <see cref="Container"/> for some use cases.
    /// </remarks>
    public class AudioContainer : AudioContainer<Drawable>
    {
    }
}
