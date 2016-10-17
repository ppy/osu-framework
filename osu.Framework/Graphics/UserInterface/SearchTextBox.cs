using System;
using System.Timers;
using osu.Framework.Events;
using osu.Framework.Graphics.Sprites;
using OpenTK;

namespace osu.Framework.Graphics.UserInterface
{
    public class SearchTextBox : TextBox
    {
        public Timer OnSearchTimer { get; set; }

        public SearchTextBox()
        {
            OnChange += SearchTextBox_OnChange;
            OnSearchTimer = new Timer(TimeSpan.FromSeconds(1).TotalMilliseconds)
            {
                AutoReset = false
            };
            OnSearchTimer.Elapsed += OnSearchTimer_Elapsed;
        }

        #region Disposal

        protected override void Dispose(bool disposing)
        {
            OnChange -= SearchTextBox_OnChange;
            OnSearchTimer.Elapsed -= OnSearchTimer_Elapsed;
            base.Dispose(disposing);
        }

        #endregion

        public override void Load(BaseGame game)
        {
            base.Load(game);
            Add(
                new Sprite
                {
                    Origin = Anchor.CentreRight,
                    Anchor = Anchor.CentreRight,
                    Texture = game.Textures.Get(@"Menu/logo"), // should be changed, but I don't have search icon texture
                    Size = new Vector2((float)(Size.Y * 0.9), (float)(Size.Y * 0.9))
                });
        }

        private void OnSearchTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            OnSearch?.Invoke(this, new OnSearchEventArgs(Text));
        }

        private void SearchTextBox_OnChange(TextBox sender, bool newText)
        {
            if (!newText) return;
            OnSearchTimer.Stop();
            OnSearchTimer.Start();
        }

        public delegate void OnSearchHandler(object sender, OnSearchEventArgs e);

        public event OnSearchHandler OnSearch;
    }
}
