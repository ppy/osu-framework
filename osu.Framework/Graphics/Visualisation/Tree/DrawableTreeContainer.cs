// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Represents the scene graph in a tree view. See <see cref="VisualisedDrawable"/>.
    /// </summary>
    internal class DrawableTreeContainer : TreeContainer<VisualisedDrawable, Drawable>
    {
        private readonly SpriteText waitingText;

        public Action? ChooseTarget;

        public DrawableTreeContainer()
        {
            AddInternal(waitingText = new SpriteText
            {
                Text = @"Waiting for target selection...",
                Font = FrameworkFont.Regular,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"choose target", () => ChooseTarget?.Invoke());
            AddButton(@"up one parent", goUpOneParent);
            AddButton(@"inspect as object", inspectAsObject);
        }

        public override Drawable? Target
        {
            set
            {
                base.Target = value;

                if (Visualiser == null)
                    Schedule(() => Visualiser!.SetTarget(value));
                else
                    Visualiser.SetTarget(value);
            }
        }

        protected override void Update()
        {
            waitingText.Alpha = Visualiser.Searching ? 1 : 0;
            base.Update();
        }

        private void goUpOneParent()
        {
            Drawable? lastHighlight = HighlightedTarget?.TargetDrawable;

            var parent = Target?.Parent;

            if (parent != null)
            {
                var lastVisualiser = TargetVisualiser!;

                Target = parent;
                ((DrawableTreeTab)TabContainer.Current).TargetDrawable = parent;
                lastVisualiser.SetContainer(TargetVisualiser);

                TargetVisualiser!.Expand();
            }

            // Rehighlight the last highlight
            if (lastHighlight != null)
            {
                VisualisedDrawable? visualised = TargetVisualiser!.FindVisualisedDrawable(lastHighlight);

                if (visualised != null)
                {
                    DrawableInspector.Show();
                    SetHighlight(visualised);
                }
            }
        }

        private void inspectAsObject()
        {
            if (Target != null)
                TabContainer.SpawnObjectVisualiser(Target);
        }

        public override VisualisedDrawable GetVisualiserFor(Drawable? drawable)
        {
            return Visualiser.GetVisualiserFor(drawable!);
        }
    }
}
