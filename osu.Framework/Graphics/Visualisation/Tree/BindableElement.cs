// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.TypeExtensions;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents an <see cref="IBindable"/> with a live view to its value
    /// and visualises its bindings.
    /// </summary>
    internal class BindableElement : ObjectElement
    {
        /// <summary>
        /// Prevents automatic unbinding from drawable unloader.
        /// </summary>
        public struct WrapBindable
        {
            public IBindable Value;
        }

        private WrapBindable targetBindable;
        public IBindable TargetBindable => targetBindable.Value;

        [NotNull]
        public override object? Target
        {
            get => TargetBindable;
            protected set => targetBindable.Value = (IBindable?)value ?? throw new ArgumentNullException(nameof(value));
        }

        private Spacer valueContainer = null!;
        private Spacer weakrefContainer = null!;
        public IBindableWrapper? LocalBindable;

        private bool inRootPosition => !(CurrentContainer is ElementNode);

        public BindableElement(IBindable bindable)
            : base(bindable)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            (valueContainer = new Spacer{ SpacerText = "Value" }).SetContainer(this);
            (weakrefContainer = new Spacer{ SpacerText = "Bindings" }).SetContainer(this);
        }

        protected override void UpdateChildren()
        {
            if (LocalBindable == null)
            {
                var t = TargetBindable.GetType();
                while (t != null && (!t.IsGenericType || t.GetGenericTypeDefinition() != typeof(Bindable<>)))
                    t = t.BaseType;
                if (t == null)
                    throw new InvalidOperationException("Type does not inherit from Bindable");
                LocalBindable = (IBindableWrapper)Activator.CreateInstance(typeof(BindableWrapper<>).MakeGenericType(t.GetGenericArguments()[0]), new object[] { TargetBindable })!;
                valueContainer.Child = Tree.GetVisualiserFor(LocalBindable.Value);

                LocalBindable.BindValueChanged(newValue =>
                    valueContainer.Child = Tree.GetVisualiserFor(newValue));
            }
            if (inRootPosition)
            {
                foreach (var binding in LocalBindable.GetWeakReferences())
                    weakrefContainer.Add(new WeakRefElement(binding));
            }
        }

        protected override Colour4 PreviewColour => Colour4.LimeGreen;

        protected override void UpdateContent()
        {
            base.UpdateContent();

            Text.Text = "Bound " + TargetBindable.GetType().ReadableName();
            Text2.Text = TargetBindable.ToString()!;
        }
        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (LocalBindable == null)
                return;

            foreach (var r in LocalBindable.GetWeakReferences())
            {
                bool isNew = true;
                foreach (var vis in weakrefContainer)
                {
                    if (vis is WeakRefElement vr && vr.IsAlive && vr.Target == r)
                    {
                        isNew = false;
                        break;
                    }
                }
                if (isNew)
                    weakrefContainer.Add(new WeakRefElement(r));
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            LocalBindable?.Unbind();
            LocalBindable = null!;
        }

        public interface IBindableWrapper
        {
            public IEnumerable<IBindable> GetWeakReferences();
            public void BindValueChanged(Action<object?> newValueAction);
            public void Unbind();
            public object? Value { get; }
        }

        private class BindableWrapper<T> : Bindable<T>, IBindableWrapper
        {
            private Bindable<T> originalBindable;
            private Lists.LockedWeakList<Bindable<T>>? targetBindings;

            public BindableWrapper(Bindable<T> bindable)
                : base()
            {
                originalBindable = bindable;
                BindTo(bindable);
            }

            public IEnumerable<IBindable> GetWeakReferences()
            {
                if (targetBindings == null)
                {
                    if (typeof(Bindable<T>).GetProperty("Bindings", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) is System.Reflection.PropertyInfo pBindings)
                    {
                        if (!(pBindings.GetValue(originalBindable) is Lists.LockedWeakList<Bindable<T>> l))
                        {
                            pBindings.SetValue(originalBindable, l = new Lists.LockedWeakList<Bindable<T>>());
                        }
                        targetBindings = l;
                    }
                }

                return targetBindings!.ToArray();
            }

            void IBindableWrapper.BindValueChanged(Action<object?> newValueAction)
            {
                BindValueChanged(v => newValueAction(v.NewValue));
            }

            public void Unbind() => UnbindAll();

            object? IBindableWrapper.Value => (object?)Value;
        }
    }
}
