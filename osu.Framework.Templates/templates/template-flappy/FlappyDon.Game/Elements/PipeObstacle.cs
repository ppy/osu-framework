using osu.Framework.Allocation;
using osu.Framework.Extensions.PolygonExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// A single obstacle for the player to overcome, consisting of two pipe sprites, one rotated 180
    /// degrees and placed along the top of the screen.
    /// </summary>
    public class PipeObstacle : CompositeDrawable
    {
        /// <summary>
        /// The vertical offset from the middle of the screen to denote the default vertical position of the gap in the pipes.
        /// </summary>
        public float VerticalPositionAdjust = -130.0f;

        private Pipe topPipe;
        private Pipe bottomPipe;

        public PipeObstacle()
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Rotate the top pipe 180 degrees and flip it horizontally so the shading matches the bottom pipe.
            topPipe = new Pipe
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.TopCentre,
                Rotation = 180.0f,
                Position = new Vector2(0.0f, -110 + VerticalPositionAdjust),
            };

            topPipe.Scale = new Vector2(-topPipe.Scale.X, topPipe.Scale.Y);

            AddInternal(topPipe);

            bottomPipe = new Pipe
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.TopCentre,
                Position = new Vector2(0.0f, 110 + VerticalPositionAdjust)
            };

            AddInternal(bottomPipe);
        }

        public bool CheckCollision(Quad birdQuad)
        {
            // Extend the top pipe bounds upwards so it's not possible to simply fly over it.
            RectangleF topPipeRect = topPipe.ScreenSpaceDrawQuad.AABBFloat;
            topPipeRect.Y -= 5000.0f;
            topPipeRect.Height += 5000.0f;
            Quad topPipeQuad = Quad.FromRectangle(topPipeRect);

            // Bird touched the top pipe
            if (birdQuad.Intersects(topPipeQuad))
                return true;

            // Bird touched the bottom pipe
            if (birdQuad.Intersects(bottomPipe.ScreenSpaceDrawQuad))
                return true;

            return false;
        }
    }
}
