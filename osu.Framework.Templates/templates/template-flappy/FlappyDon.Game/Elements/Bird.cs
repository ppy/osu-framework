using System;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Animations;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
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
        public float GroundY = 0.0f;
        public bool IsTouchingGround { get; private set; }

        private readonly TextureAnimation animation = new TextureAnimation();

        [Resolved]
        private TextureStore textures { get; set; }

        private DrawableSample flapSound;

        private bool isPlaying;
        private float currentVelocity;

        [BackgroundDependencyLoader]
        private void load(ISampleStore samples)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.Centre;
            Position = new Vector2(120.0f, .0f);

            animation.AddFrame(textures.Get("redbird-upflap"), 200.0f);
            animation.AddFrame(textures.Get("redbird-downflap"), 200.0f);
            animation.AddFrame(textures.Get("redbird-midflap"), 200.0f);
            animation.Origin = Anchor.Centre;
            animation.Anchor = Anchor.Centre;
            animation.Scale = new Vector2(0.5f);

            AddInternal(animation);
            AddInternal(flapSound = new DrawableSample(samples.Get("wing.ogg")));

            Size = new Vector2(50.0f, 35.0f);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Reset();
        }

        public void Reset()
        {
            isPlaying = false;
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

        public void FallDown()
        {
            currentVelocity = 0.0f;
            animation.IsPlaying = false;
            animation.GotoFrame(2);
        }

        public void FlyUp()
        {
            if (!isPlaying)
            {
                isPlaying = true;
                ClearTransforms();
            }

            animation.GotoFrame(0);

            currentVelocity = 70.0f;
            this.RotateTo(-45.0f).Then(350.0f).RotateTo(90.0f, 500.0f);
            flapSound.Play();
        }

        protected override void Update()
        {
            if (!isPlaying)
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
