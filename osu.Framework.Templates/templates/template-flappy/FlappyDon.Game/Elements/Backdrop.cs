using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// Manages a pool of Sprite objects, arranged and animated horizontally to produce a background scrolling effect.
    /// </summary>
    public class Backdrop : CompositeDrawable
    {
        /// <summary>
        /// Holds a lambda that generates new instances of the same sprite on demand.
        /// </summary>
        private readonly Func<Sprite> createSprite;

        /// <summary>
        /// The previous size of this container, used to determine when we need to perform a fresh layout.
        /// </summary>
        private Vector2 lastSize;

        /// <summary>
        /// Whether the sprite set is currently animating or not.
        /// </summary>
        public bool Running { get; private set; }

        /// <summary>
        /// The duration it takes to animate one cycle of the sprites in the container.
        /// </summary>
        public readonly double Duration;

        /// <summary>
        /// Create a new instance of a backdrop element, supplying a function to dynamically generate multiple copies of one sprite,
        /// and an animation value dictating the scrolling speed of the whole element.
        /// </summary>
        /// <param name="createSprite">A function that will create and return the same Sprite object for this container to manage.</param>
        /// <param name="duration">The horizontal speed from right to left that the sprites will scroll at.</param>
        public Backdrop(Func<Sprite> createSprite, double duration = 2000.0f)
        {
            this.createSprite = createSprite;
            Duration = duration;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            Size = new Vector2(1.0f);

            // Add an initial sprite from which we can get the aspect ratio
            AddInternal(createSprite());
        }

        /// <summary>
        /// Start animating the child sprites across the screen
        /// </summary>
        public void Start()
        {
            if (Running)
                return;

            Running = true;
            updateLayout();
        }

        /// <summary>
        /// Cancel the animations, but leave all sprites in place.
        /// </summary>
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

            // If the bounds of this container changed, add or remove the number of child sprites to fill the visible space.
            if (!DrawSize.Equals(lastSize))
            {
                updateLayout();
                lastSize = DrawSize;
            }
        }

        private void updateLayout()
        {
            // Get an initial sprite we can use to derive size
            Sprite sprite = (Sprite)InternalChildren.First();

            // Work out how many copies are needed to horizontally fill the screen
            var spriteNum = (int)Math.Ceiling(DrawWidth / sprite.DrawWidth) + 1;

            // If the number needed is higher or lower than the current number of child sprites, add/remove the amount needed for them to match
            if (spriteNum != InternalChildren.Count)
            {
                // Update the number of sprites in the list to match the number we need to cover the whole container
                while (InternalChildren.Count > spriteNum)
                    RemoveInternal(InternalChildren.Last());

                while (InternalChildren.Count < spriteNum)
                    AddInternal(createSprite());
            }

            // Lay out all of the child sprites horizontally, and assign a looping pan animation to create the effect of constant scrolling
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

        private void stopAnimatingChildren() => ClearTransforms(true);
    }
}
