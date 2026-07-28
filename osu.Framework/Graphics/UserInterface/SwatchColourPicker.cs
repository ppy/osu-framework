// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace osu.Framework.Graphics.UserInterface
{
    /// <summary>
    /// A row of clickable colour presets for use in a <see cref="ColourPicker"/>.
    /// Hidden when <see cref="Colours"/> is empty.
    /// </summary>
    public abstract partial class SwatchColourPicker : CompositeDrawable, IHasCurrentValue<Colour4>
    {
        private readonly BindableWithCurrent<Colour4> current = new BindableWithCurrent<Colour4>();

        public Bindable<Colour4> Current
        {
            get => current.Current;
            set => current.Current = value;
        }

        /// <summary>
        /// The preset colours to display as swatches.
        /// </summary>
        public BindableList<Colour4> Colours { get; }

        /// <summary>
        /// The background of the control.
        /// </summary>
        protected Box Background { get; }

        /// <summary>
        /// Contains the swatch drawables.
        /// </summary>
        protected FillFlowContainer Content { get; }

        protected SwatchColourPicker(BindableList<Colour4>? colours = null)
        {
            Colours = colours ?? new BindableList<Colour4>();

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                Background = new Box
                {
                    RelativeSizeAxes = Axes.Both
                },
                Content = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Spacing = new Vector2(5),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                }
            };
        }

        public override bool IsPresent => base.IsPresent && Colours.Count > 0;

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Colours.BindCollectionChanged(collectionChanged, true);
        }

        private void collectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                {
                    var newItems = e.NewItems.AsNonNull().Cast<Colour4>().ToArray();
                    int startIndex = Math.Max(e.NewStartingIndex, 0);

                    for (int i = 0; i < newItems.Length; i++)
                        insertSwatch(newItems[i], startIndex + i);

                    break;
                }

                case NotifyCollectionChangedAction.Remove:
                {
                    if (e.OldStartingIndex < 0)
                    {
                        // No index available (e.g. RemoveAll), so rebuild.
                        Content.Clear(true);

                        for (int i = 0; i < Colours.Count; i++)
                            insertSwatch(Colours[i], i);

                        break;
                    }

                    for (int i = 0; i < e.OldItems.AsNonNull().Count; i++)
                        removeSwatchAt(e.OldStartingIndex);

                    break;
                }

                case NotifyCollectionChangedAction.Replace:
                {
                    int count = e.OldItems.AsNonNull().Count;

                    for (int i = 0; i < count; i++)
                        removeSwatchAt(e.OldStartingIndex);

                    var newItems = e.NewItems.AsNonNull().Cast<Colour4>().ToArray();

                    for (int i = 0; i < newItems.Length; i++)
                        insertSwatch(newItems[i], e.NewStartingIndex + i);

                    break;
                }

                case NotifyCollectionChangedAction.Move:
                {
                    var flowing = Content.FlowingChildren.ToList();
                    var drawable = flowing[e.OldStartingIndex];

                    flowing.RemoveAt(e.OldStartingIndex);
                    flowing.Insert(e.NewStartingIndex, drawable);

                    for (int i = 0; i < flowing.Count; i++)
                        Content.SetLayoutPosition(flowing[i], i);

                    break;
                }

                case NotifyCollectionChangedAction.Reset:
                    Content.Clear(true);
                    break;
            }
        }

        private void insertSwatch(Colour4 colour, int index)
        {
            var flowing = Content.FlowingChildren.ToList();

            for (int i = index; i < flowing.Count; i++)
                Content.SetLayoutPosition(flowing[i], i + 1);

            var swatch = CreateSwatch(colour).With(s => s.Anchor = s.Origin = Anchor.TopCentre);
            swatch.Action = () => Current.Value = colour;
            Content.Insert(index, swatch);
        }

        private void removeSwatchAt(int index)
        {
            var flowing = Content.FlowingChildren.ToList();
            var drawable = flowing[index];

            for (int i = index + 1; i < flowing.Count; i++)
                Content.SetLayoutPosition(flowing[i], i - 1);

            Content.Remove(drawable, true);
        }

        /// <summary>
        /// Creates a clickable swatch for the given colour.
        /// </summary>
        protected abstract ClickableContainer CreateSwatch(Colour4 colour);
    }
}
