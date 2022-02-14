using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Sample;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Textures;
using osuTK;

namespace FlappyDon.Game.Elements
{
    /// <summary>
    /// The Sprite element controlled by the player. Plays a looping 'flapping' texture sequence, which
    /// is reset each time the player interacts with it.
    /// </summary>
    public class Bird : CompositeDrawable
    {
        /// <summary>
        /// In the relative space, the absolute position of the ground that when crossed will register as a collision.
        /// </summary>
        public float GroundY = 0.0f;

        /// <summary>
        /// Will return true once the bird has passed the GroundY value.
        /// </summary>
        public bool IsTouchingGround { get; private set; }

        /// <summary>
        /// The quad value representing this sprite for collision purposes.
        /// </summary>
        public Quad CollisionQuad
        {
            get
            {
                // Because the quad is rotated 45 degrees during animation and the collision
                // event occurs when even the corner touches, shrink the collision box
                // so that it more accurately matches the bounds of the character on-screen.
                RectangleF rect = ScreenSpaceDrawQuad.AABBFloat;
                rect = rect.Shrink(new Vector2(rect.Width * 0.3f, rect.Height * 0.2f));
                return Quad.FromRectangle(rect);
            }
        }

        private TextureAnimation animation;

        [Resolved]
        private TextureStore textures { get; set; }

        private DrawableSample flapSound;

        private bool isIdle;

        private float currentVelocity;

        [BackgroundDependencyLoader]
        private void load(ISampleStore samples)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.Centre;
            Position = new Vector2(120.0f, .0f);

            animation = new TextureAnimation
            {
                Origin = Anchor.Centre,
                Anchor = Anchor.Centre,
            };

            animation.AddFrame(textures.Get("redbird-upflap"), 200.0f);
            animation.AddFrame(textures.Get("redbird-downflap"), 200.0f);
            animation.AddFrame(textures.Get("redbird-midflap"), 200.0f);

            AddInternal(animation);
            AddInternal(flapSound = new DrawableSample(samples.Get("wing.ogg")));

            Size = animation.Size;
            Scale = new Vector2(0.45f);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Reset();
        }

        /// <summary>
        /// Reset the animation and rotation state of the bird.
        /// </summary>
        public void Reset()
        {
            isIdle = true;
            IsTouchingGround = false;
            ClearTransforms();
            Rotation = 0.0f;
            Y = -60.0f;
            animation.IsPlaying = true;
            this.Loop(b => b.MoveToOffset(new Vector2(0.0f, -20.0f), 1000.0f, Easing.InOutSine)
                            .Then()
                            .MoveToOffset(new Vector2(0.0f, 20.0f), 1000.0f, Easing.InOutSine)
            );
        }

        /// <summary>
        /// Halt all animations and velocity so the bird will promptly start falling.
        /// </summary>
        public void FallDown()
        {
            currentVelocity = 0.0f;
            animation.IsPlaying = false;
            animation.GotoFrame(2);
        }

        /// <summary>
        /// Called on each screen tap, increment the velocity slightly to make the bird fly up vertically.
        /// </summary>
        public void FlyUp()
        {
            if (isIdle)
            {
                isIdle = false;
                ClearTransforms();
            }

            animation.GotoFrame(0);

            currentVelocity = 70.0f;
            this.RotateTo(-45.0f).Then(350.0f).RotateTo(90.0f, 500.0f);
            flapSound.Play();
        }

        protected override void Update()
        {
            if (isIdle)
            {
                base.Update();
                return;
            }

            currentVelocity -= (float)Clock.ElapsedFrameTime * 0.22f;
            Y -= currentVelocity * (float)Clock.ElapsedFrameTime * 0.01f;

            float groundPlane;
            if (GroundY > 0.0f)
                groundPlane = GroundY / 2.0f;
            else
                groundPlane = Parent.DrawHeight - DrawHeight;

            Y = Math.Min(Y, groundPlane);

            if (Y >= groundPlane)
            {
                IsTouchingGround = true;
                ClearTransforms();
            }

            base.Update();
        }
    }
}
