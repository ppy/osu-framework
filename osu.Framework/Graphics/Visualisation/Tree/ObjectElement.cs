// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections;

using osu.Framework.Input.Events;
using osu.Framework.Bindables;
using osuTK.Graphics;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents an arbitrary object as a node, to allow deeper inspection.
    /// </summary>
    internal class ObjectElement : ElementNode
    {
        public override object? Target { get; protected set; }
        protected virtual Colour4 PreviewColour =>
            Target is Drawable
            ? Color4.White
            : Target is ValueType
            ? Color4.DarkViolet
            : Color4.Blue;

        public ObjectElement(object? obj)
            : base(obj)
        {
        }

        protected override void UpdateContent()
        {
            PreviewBox.Alpha = 1;
            PreviewBox.Colour = PreviewColour;

            Text.Text = Target!.ToString()!;
            if (Target is IList arr)
            {
                Text2.Text = !IsExpanded ? $"({arr.Count} elements)" : string.Empty;
            }
            else
            {
                Text2.Text = string.Empty;
            }

            Alpha = 1;
        }

        protected override void UpdateChildren()
        {
        }

        public static ObjectElement CreateFor(object? element)
        {
            if (element is null)
                return NullVisualiser.Instance;
            else if (element is IEnumerable en && !(element is string) && !(element is IDrawable))
                return ListElement.Create(en);
            else if (element is IBindable b)
                return new BindableElement(b);
            else
                return new ObjectElement(element);
        }

        private class NullVisualiser : ObjectElement
        {
            private NullVisualiser()
                : base(null)
            {
            }

            public static NullVisualiser Instance => new NullVisualiser();

            protected override Colour4 PreviewColour => Colour4.Gray;
            protected override void UpdateContent()
            {
                PreviewBox.Alpha = 1;
                PreviewBox.Colour = PreviewColour;

                Text.Text = "<<NULL>>";
                Text2.Text = string.Empty;
            }

            protected override bool OnDoubleClick(DoubleClickEvent e) => true;
        }
    }
}
