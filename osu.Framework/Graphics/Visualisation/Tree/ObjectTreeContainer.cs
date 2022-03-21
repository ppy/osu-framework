// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;

#nullable enable

namespace osu.Framework.Graphics.Visualisation.Tree
{
    /// <summary>
    /// Allows inspecting arbitrary objects by representing them in a graph.
    /// </summary>
    internal class ObjectTreeContainer : TreeContainer<ObjectElement, object>
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddButton(@"inspect root", inspectRoot);
            AddButton(@"inspect as drawable", inspectAsDrawable);
        }


        [Resolved]
        private Game game { get; set; } = null!;

        private void inspectRoot()
        {
            _ = TabContainer.SpawnObjectVisualiser(game);
        }

        private void inspectAsDrawable()
        {
            ElementNode? element = HighlightedTarget ?? TargetVisualiser;
            if (element?.Target is Drawable d)
            {
                Visualiser.Target = d;
            }
        }

        public override ObjectElement GetVisualiserFor(object? element)
        {
            return ObjectElement.CreateFor(element);
        }
    }
}
