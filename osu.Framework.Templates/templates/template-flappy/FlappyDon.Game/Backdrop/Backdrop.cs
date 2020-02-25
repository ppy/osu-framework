// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace FlappyDon.Game
{
    /// <summary>
    /// Manages a pool of Sprite objects,
    /// arranged and animated horizontally
    /// to produce a background scrolling effect.
    /// </summary>
    public class Backdrop : CompositeDrawable
    {
        // Holds a lambda that generates new
        // instances of the same sprite on demand.
        private readonly Func<Sprite> _createSprite;

        // The previous size of this container,
        // used to determine when we need to
        // perform a fresh layout
        private Vector2 lastSize;

        // Whether the sprite set is currently
        // animating or not
        public bool Running { get; private set; }

        // The duration it takes to animate one cycle
        // of the sprites in the container.
        public float Duration;

        public Backdrop(Func<Sprite> createSprite, float duration = 2000.0f)
        {
            _createSprite = createSprite;
            Duration = duration;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Size = new Vector2(1.0f);
        }

        public void Start()
        {
            if (Running)
                return;

            Running = true;
            updateChildrenList();
        }

        public void Freeze()
        {
            if (!Running)
                return;

            Running = false;
            stopAnimatingChildren();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            // If the bounds of this container changed,
            // add or remove the number of child sprites
            // to fill the visible space.
            if (DrawSize.Equals(lastSize) == false)
            {
                updateChildrenList();
                lastSize = DrawSize;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateChildrenList();
        }

        private void updateChildrenList()
        {
            // Get an initial sprite we can use to derive size
            Sprite sprite;

            // Fetch an initial child sprite
            // we can use to measure
            if (InternalChildren.Count > 0)
                sprite = (Sprite)InternalChildren.First();
            else
            {
                sprite = _createSprite();
                AddInternal(sprite);
            }

            // Work out how many copies are needed to horizontally fill the screen
            var spriteNum = (int)Math.Ceiling(DrawWidth / sprite.DrawWidth) + 1;

            // If the number needed is higher or lower than
            // the current number of child sprites, add/remove
            // the amount needed for them to match.
            if (spriteNum != InternalChildren.Count)
            {
                // Update the number of sprites in the list to match
                // the number we need to cover the whole container
                while (InternalChildren.Count > spriteNum)
                    RemoveInternal(InternalChildren.Last());

                while (InternalChildren.Count < spriteNum)
                    AddInternal(_createSprite());
            }

            // Lay out all of the child sprites horizontally,
            // and assign a looping pan animation to create the effect
            // of constant scrolling.
            var offset = 0.0f;

            foreach (var childSprite in InternalChildren)
            {
                var width = childSprite.DrawWidth * sprite.Scale.X;
                childSprite.Position = new Vector2(offset, childSprite.Position.Y);

                var fromVector = new Vector2(offset, childSprite.Position.Y);
                var toVector = new Vector2(offset - width, childSprite.Position.Y);

                if (Running)
                    childSprite.Loop(b => b.MoveTo(fromVector).MoveTo(toVector, Duration));

                // Change the inset to overlap slightly in order to avoid potential seam lines
                offset += width - 1;
            }
        }

        private void stopAnimatingChildren()
        {
            foreach (var childSprite in InternalChildren)
                childSprite.ClearTransforms();
        }
    }
}
