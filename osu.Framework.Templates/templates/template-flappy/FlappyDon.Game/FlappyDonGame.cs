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
        private readonly ScoreSpriteText scoreSpriteText = new ScoreSpriteText();

        // Game elements
        private readonly Bird bird = new Bird();
        private readonly Obstacles obstacles = new Obstacles();

        // Background elements
        private Backdrop skyBackdrop;
        private Backdrop groundBackdrop;

        // Sound effects
        private DrawableSample scoreSound;
        private DrawableSample punchSound;
        private DrawableSample fallSound;
        private DrawableSample whooshSound;

        // Game state
        private int score;
        private bool gameOver;
        private bool gameOverCooldown;

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
                scoreSpriteText,
                screenFlash
            };

            // Configure the sizing strategy in such a way that all elements
            // are relatively scaled in contrast to the Y-axis (ie height of the window),
            // but changing the X-axis (ie window width) has no effect on scaling.
            gameScreen.Strategy = DrawSizePreservationStrategy.Minimum;
            gameScreen.TargetDrawSize = new Vector2(0, 768);
            AddInternal(gameScreen);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Set the Y offset from the top
            // that counts as the ground for the bird
            bird.GroundY = 525.0f;

            // Inform the obstacles the position of the
            // bird in order to detect when the player successfully
            // earns a point
            obstacles.BirdThreshold = bird.X;

            // Start animating the background elements
            groundBackdrop.Start();
            skyBackdrop.Start();

            // Update the screen UI to show only the launch art
            scoreSpriteText.Hide();
            gameOverSprite.Hide();
            logoSprite.Show();
        }

        private void reset()
        {
            gameOver = false;

            // Reset score
            score = 0;
            scoreSpriteText.Text = "0";

            // Play reset noise
            whooshSound.Play();

            // Flash screen to hide the
            // UI/backdrop element transitions
            screenFlash.ResetFlash();

            // Reset state of game elements
            bird.Reset();
            obstacles.Reset();

            // Restart the backdrop elements
            groundBackdrop.Start();
            skyBackdrop.Start();

            // Reset the UI elements
            scoreSpriteText.Hide();
            gameOverSprite.Hide();
            logoSprite.Show();
        }

        private void fail()
        {
            // Play a brief flash to make the hit very visible
            screenFlash.GameOverFlash();

            // Play the punch sound, and then the 'fall' sound slightly after
            punchSound.Play();
            Scheduler.AddDelayed(() => fallSound.Play(),
                70.0);

            // Show the game over title in the middle of the flash
            gameOverSprite.Show(screenFlash.FlashDuration);

            // Animate the bird falling down to the ground
            bird.FallDown();

            // Freeze all other moving elements on screen
            obstacles.Freeze();
            groundBackdrop.Freeze();
            skyBackdrop.Freeze();

            // Set a flag to block input for half a second
            // so the user can't accidentally reset the game instantly
            // after hitting a pipe.
            gameOverCooldown = true;
            Scheduler.AddDelayed(() => gameOverCooldown = false, 500.0f);
        }

        protected override void Update()
        {
            if (gameOver)
                return;

            // Register a collision if the bird hits a pipe or the ground
            if (obstacles.CollisionDetected(bird.ScreenSpaceDrawQuad)
                || bird.IsTouchingGround)
            {
                gameOver = true;
                fail();
                return;
            }

            // If a pipe crosses the bird's X offset,
            // (And no collisions occurred),
            // increment the score by 1
            if (obstacles.ThresholdCrossed())
            {
                score++;
                scoreSpriteText.Text = score.ToString();
                scoreSound.Play();
            }

            base.Update();
        }

        private void onTapEvent()
        {
            // Begin game play
            obstacles.Start();
            logoSprite.Hide();
            scoreSpriteText.Show();

            // Animate the bird flying up
            bird.FlyUp();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            // After dying, disable input briefly to stop the user
            // restarting the game too quickly.
            if (gameOverCooldown)
                return base.OnKeyDown(e);

            if (gameOver)
            {
                reset();
                return base.OnKeyDown(e);
            }

            if (e.Key == Key.Space && e.Repeat == false)
                onTapEvent();

            return base.OnKeyDown(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            // Since some devices rely on top or bottom swipe touches,
            // (eg, swipe-to-close on iPhone X),
            // disregard events around those areas
            float verticalOffset = e.MouseDownPosition.Y / DrawHeight;
            if (verticalOffset < 0.05f || verticalOffset > 0.95f)
                return base.OnMouseDown(e);

            if (gameOverCooldown)
                return base.OnMouseDown(e);

            if (gameOver)
            {
                reset();
                return base.OnMouseDown(e);
            }

            onTapEvent();

            return base.OnMouseDown(e);
        }
    }
}
