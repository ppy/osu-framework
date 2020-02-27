using osu.Framework.Allocation;
using osu.Framework.Extensions.PolygonExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osuTK;

namespace FlappyDon.Game
{
    /// <summary>
    /// A single obstacle for the player to overcome,
    /// consisting of two pipe sprites, one rotated 180
    /// degrees and placed along the top of the screen.
    /// </summary>
    public class PipeObstacle : CompositeDrawable
    {
        public bool Scored = false;

        public float Offset = -130.0f;

        private Pipe topPipe;
        private Pipe bottomPipe;

        [BackgroundDependencyLoader]
        private void load()
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            RelativeSizeAxes = Axes.Y;
            AutoSizeAxes = Axes.X;

            // Rotate the top pipe 180 degrees, and flip
            // it horizontally so the shading matches the bottom pipe
            topPipe = new Pipe();
            topPipe.Rotation = 180.0f;
            topPipe.Scale = new Vector2(-topPipe.Scale.X, topPipe.Scale.Y);
            topPipe.Anchor = Anchor.Centre;
            topPipe.Origin = Anchor.TopCentre;
            topPipe.Position = new Vector2(0.0f, -110 + Offset);
            AddInternal(topPipe);

            bottomPipe = new Pipe();
            bottomPipe.Anchor = Anchor.Centre;
            bottomPipe.Origin = Anchor.TopCentre;
            bottomPipe.Position = new Vector2(0.0f, 110 + Offset);
            AddInternal(bottomPipe);
        }

        public bool Colliding(Quad birdQuad)
        {
            // Extend the top pipe bounds upwards so
            // it's not possible to simply fly over it.
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
