using System.Diagnostics;
using FlappyDon.Game.Elements;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Input;

namespace FlappyDon.Game
{
    public class FlappyDonGame : FlappyDonGameBase
    {
        // The main container for holding all of the game content
        private readonly DrawSizePreservingFillContainer gameScreen = new DrawSizePreservingFillContainer();

        // Front-screen UI elements
        private readonly TitleSprite gameOverSprite = new TitleSprite("gameover");
        private readonly TitleSprite logoSprite = new TitleSprite("message");
        private readonly ScreenFlash screenFlash = new ScreenFlash();

        // Game elements
        private readonly Bird bird = new Bird();
        private readonly Obstacles obstacles = new Obstacles();

        // Background elements
        private Backdrop skyBackdrop;
        private Backdrop groundBackdrop;

        // Score Counter
        private readonly ScoreCounter scoreCounter = new ScoreCounter();

        // Sound effects
        private DrawableSample scoreSound;
        private DrawableSample punchSound;
        private DrawableSample fallSound;
        private DrawableSample whooshSound;

        // Game state
        private GameState state = GameState.Ready;
        private bool disableInput;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Load the sound effects
            Add(scoreSound = new DrawableSample(Audio.Samples.Get("point.ogg")));
            Add(punchSound = new DrawableSample(Audio.Samples.Get("hit.ogg")));
            Add(fallSound = new DrawableSample(Audio.Samples.Get("die.ogg")));
            Add(whooshSound = new DrawableSample(Audio.Samples.Get("swoosh.ogg")));

            // Create and configure the background elements
            skyBackdrop = new Backdrop(() => new BackdropSprite(), 20000.0f);
            groundBackdrop = new Backdrop(() => new GroundSprite(), 2250.0f);

            // Add all of the sprites to the game window
            gameScreen.Children = new Drawable[]
            {
                skyBackdrop,
                obstacles,
                bird,
                groundBackdrop,
                gameOverSprite,
                logoSprite,
                scoreCounter.ScoreSpriteText,
                screenFlash
            };

            // Configure the sizing strategy in such a way that all elements are relatively scaled in contrast to the Y-axis (ie height of the window),
            // but changing the X-axis (ie window width) has no effect on scaling.
            gameScreen.Strategy = DrawSizePreservationStrategy.Minimum;
            gameScreen.TargetDrawSize = new Vector2(0, 768);
            AddInternal(gameScreen);

            // Register a method to be triggered each time the bird crosses a pipe threshold
            obstacles.ThresholdCrossed = _ =>
            {
                scoreCounter.IncrementScore();
                scoreSound.Play();
            };

            // Set the Y offset from the top that counts as the ground for the bird
            bird.GroundY = 525.0f;

            // Inform the obstacles the position of the bird in order to detect when the player successfully earns a point
            obstacles.BirdThreshold = bird.X;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            ready();
        }

        protected override void Update()
        {
            base.Update();

            switch (state)
            {
                case GameState.Playing:
                    // Register a collision if the bird hits a pipe or the ground
                    if (obstacles.CheckForCollision(bird.CollisionQuad) || bird.IsTouchingGround)
                        changeGameState(GameState.GameOver);
                    break;
            }
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Repeat)
                return base.OnKeyDown(e);

            if (e.Key == Key.Space && handleTap())
                // Return true to denote we captured the input here, so we don't need to continue the chain
                return true;

            return base.OnKeyDown(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            // Since some devices rely on top or bottom swipe touches, (eg, swipe-to-close on iPhone X),
            // disregard events around those areas
            float verticalOffset = e.MouseDownPosition.Y / DrawHeight;
            if (verticalOffset < 0.05f || verticalOffset > 0.95f)
                return base.OnMouseDown(e);

            if (handleTap())
                // Return true to denote we captured the input here, so we don't need to continue the chain
                return true;

            return base.OnMouseDown(e);
        }

        /// <summary>
        /// Handles all of the commonly shared input logic between mouse clicks,
        /// button pushes and screen taps
        /// </summary>
        /// <returns>Returns true if successfully handled.</returns>
        private bool handleTap()
        {
            // After dying, disable input briefly to stop the user restarting the game too quickly.
            if (disableInput)
                return false;

            switch (state)
            {
                case GameState.GameOver:
                    reset();
                    return true;

                case GameState.Playing:
                    // Animate the bird flying up
                    bird.FlyUp();
                    return true;

                default:
                    // Start the game
                    changeGameState(GameState.Playing);
                    return true;
            }
        }

        private void reset() => changeGameState(GameState.Ready);

        private void changeGameState(GameState newState)
        {
            if (newState == state)
                return;

            state = newState;

            switch (newState)
            {
                case GameState.Ready:
                    ready();
                    break;

                case GameState.Playing:
                    play();
                    break;

                case GameState.GameOver:
                    fail();
                    break;
            }
        }

        private void ready()
        {
            Debug.Assert(state == GameState.Ready);

            // Reset score
            scoreCounter.Reset();

            // Play reset noise
            whooshSound.Play();

            // Flash screen to hide the UI/backdrop element transitions
            screenFlash.Flash(0.0, 700.0);

            // Reset state of game elements
            bird.Reset();
            obstacles.Reset();

            // Restart the backdrop elements
            groundBackdrop.Start();
            skyBackdrop.Start();

            // Reset the UI elements
            gameOverSprite.Hide();
            logoSprite.Show();
        }

        private void play()
        {
            Debug.Assert(state == GameState.Playing);

            obstacles.Start();
            logoSprite.Hide();
            scoreCounter.ScoreSpriteText.Show();

            // the bird should always start flying up when the game starts, ready for the player to take over.
            bird.FlyUp();
        }

        private void fail()
        {
            Debug.Assert(state == GameState.GameOver);

            const double fade_in_duration = 30.0;

            // Play a brief flash to make the hit very visible and show the game over text
            // at the peak of the flash
            screenFlash.Flash(fade_in_duration, 500.0);
            Scheduler.AddDelayed(() => gameOverSprite.Show(), fade_in_duration);

            // Play the punch sound, and then the 'fall' sound slightly after
            punchSound.Play();
            Scheduler.AddDelayed(() => fallSound.Play(), 70.0);

            // Animate the bird falling down to the ground
            bird.FallDown();

            // Freeze all other moving elements on screen
            obstacles.Freeze();
            groundBackdrop.Freeze();
            skyBackdrop.Freeze();

            // Set a flag to block input for half a second so the user can't
            // accidentally reset the game instantly after hitting a pipe.
            disableGameInput(500.0f);
        }

        private void disableGameInput(double duration)
        {
            disableInput = true;
            Scheduler.AddDelayed(() => disableInput = false, duration);
        }
    }
}
