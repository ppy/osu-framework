// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics.CodeAnalysis;
using osu.Framework.Extensions.TypeExtensions;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents a weak reference to an object, so that its presence in the graph
    /// does not prevent its target from collection.
    /// </summary>
    internal class WeakRefElement : ObjectElement
    {
        public WeakReference TargetWeakRef { get; private set; } = null!;

        public new bool IsAlive => TargetWeakRef.IsAlive;

        [NotNull]
        public override object? Target
        {
            get => TargetWeakRef.Target ?? throw new InvalidOperationException("This visualiser holds a null reference");
            protected set => TargetWeakRef = new WeakReference(value ?? throw new ArgumentNullException(nameof(value)));
        }

        public WeakRefElement(object obj)
            : base(obj)
        {}

        protected override void Update()
        {
            if (!IsAlive)
            {
                CurrentContainer?.RemoveVisualiser(this);
                return;
            }

            base.Update();
        }

        protected override Colour4 PreviewColour => Colour4.Brown;

        protected override void UpdateContent()
        {
            base.UpdateContent();

            Text.Text = "Weak " + Target.GetType().ReadableName();
            Text2.Text = Target.ToString()!;
        }
    }
}
