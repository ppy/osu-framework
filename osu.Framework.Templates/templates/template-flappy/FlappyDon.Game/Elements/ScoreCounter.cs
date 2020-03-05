namespace FlappyDon.Game.Elements
{
    public class ScoreCounter
    {
        /// <summary>
        /// The current number of points the player has. This is the source of
        /// truth for all of the score state in the game.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// A text sprite that shows the current score.
        /// </summary>
        public readonly ScoreSpriteText ScoreSpriteText = new ScoreSpriteText();

        public void Reset()
        {
            Score = 0;
            ScoreSpriteText.Text = "0";
            ScoreSpriteText.Hide();
        }

        public void Start()
        {
            ScoreSpriteText.Show();
        }

        public void IncrementScore()
        {
            Score++;
            ScoreSpriteText.Text = Score.ToString();
        }
    }
}
