// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osuTK;

namespace FlappyDon.Game
{
    /// <summary>
    /// A full-screen container used to manage
    /// a pool of pipe obstacles. Dequeues pipes
    /// when they go off screen, and creates new instances
    /// as needed. Also handles collision detection between
    /// any pipes and the bird sprite.
    /// </summary>
    public class Obstacles : CompositeDrawable
    {
        // The bounding size of the collision box
        // representing the bird
        public Vector2 CollisionBoxSize = new Vector2(50);

        // Whether the obstacles are presently
        // animating or not.
        public bool Running { get; private set; }

        // A horizontal X value from the left that
        // indicates where the bird is on the X-axis.
        // When a pipe crosses over this threshold, it
        // counts as a point to the player.
        public float BirdThreshold = 0.0f;

        // If the pipes are visible on screen, but their
        // animation has been stopped.
        private bool frozen;

        private float pipeVelocity
        {
            get
            {
                if (Clock.FramesPerSecond > 0.0f)
                    return 185.0f * (float)(Clock.ElapsedFrameTime / 1000.0f);

                return 0.0f;
            }
        }

        private const float pipeDistance = 350.0f;

        public Obstacles()
        {
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            RelativeSizeAxes = Axes.Both;
        }

        public void Start()
        {
            if (Running)
                return;

            Running = true;
        }

        public void Freeze()
        {
            if (!Running)
                return;

            frozen = true;
        }

        public void Reset()
        {
            if (!Running) return;

            Running = false;
            frozen = false;

            // Remove all child pipes
            // from this container
            ClearInternal();
        }

        public bool CollisionDetected(Quad birdQuad)
        {
            if (InternalChildren.Count == 0)
                return false;

            var obstacle = (PipeObstacle)InternalChildren.First();
            return obstacle.Colliding(birdQuad);
        }

        public bool ThresholdCrossed()
        {
            if (InternalChildren.Count == 0) return false;

            // Only check the first pipe since no other pipes would
            // be in range yet
            var first = (PipeObstacle)InternalChildren.First();

            // Disregard if the pipe object already registers as crossed
            if (first.Scored)
                return false;

            // If the pipe has moved to in front of the bird,
            // register as a new point.
            if (first.X < BirdThreshold)
            {
                first.Scored = true;
                return true;
            }

            return false;
        }

        protected override void Update()
        {
            if (!Running) return;

            if (InternalChildren.Count == 0)
            {
                spawnNewObstacle();
                return;
            }

            // Update the position of each pipe obstacle, and de-queue ones that go off screen
            foreach (var drawable in InternalChildren)
            {
                if (frozen)
                    break;

                var obstacle = (PipeObstacle)drawable;
                obstacle.Position = new Vector2(obstacle.Position.X - pipeVelocity, 0.0f);
                if (obstacle.Position.X + obstacle.DrawWidth < 0.0f)
                    RemoveInternal(obstacle);
            }

            // Spawn a new pipe when sufficient distance has passed
            if (InternalChildren.Count > 0)
            {
                var lastObstacle = (PipeObstacle)InternalChildren.Last();
                if (lastObstacle.Position.X + lastObstacle.DrawWidth < DrawWidth - pipeDistance)
                    spawnNewObstacle();
            }
            else
                spawnNewObstacle();
        }

        private void spawnNewObstacle()
        {
            var obstacle = new PipeObstacle();
            obstacle.Position = new Vector2(DrawWidth, 0.0f);
            obstacle.Offset = RNG.NextSingle(-140.0f, 60.0f);
            AddInternal(obstacle);
        }
    }
}
